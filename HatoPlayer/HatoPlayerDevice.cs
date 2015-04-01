using HatoBMSLib;
using HatoDSP;
using HatoLib;
using HatoSound;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoPlayer
{
    public class HatoPlayerDevice
    {
        //************* 設定 *************
        public bool DefaultKeySound = true;

        //************* デバイス *************
        BMSStruct b;
        HatoSoundDevice hsound;

        //************* データとか *************
        Dictionary<int, SecondaryBuffer> WavidToBuffer = new Dictionary<int, SecondaryBuffer>();
        Dictionary<int, HatoSynthDevice> MixchToSynth = new Dictionary<int, HatoSynthDevice>();

        SecondaryBuffer defkey;

        public HatoPlayerDevice(Form form, BMSStruct b)
        {
            this.b = b;
            hsound = new HatoSoundDevice(form);

            defkey = new SecondaryBuffer(hsound, HatoPath.FromAppDir("key.ogg"));
        }

        /// <summary>
        /// wavまたはoggファイルが存在した場合にファイルを同期的に読み込みます。
        /// キー音を読み込むには、PrepareSound()の方を使用して下さい。
        /// </summary>
        /// <param name="fullpath">wavまたはoggファイルの、拡張子付きのフルパス。</param>
        /// <returns></returns>
        public SecondaryBuffer LoadAudioFileOrGoEasy(string fullpath)
        {
            if (AudioFileReader.FileExists(fullpath))
            {
                return new SecondaryBuffer(hsound, fullpath);
            }
            else
            {
                return new SecondaryBuffer(hsound);
            }
        }

        public long AudioFileSize(int wavid)
        {
            string fn;

            if (!b.WavDefinitionList.TryGetValue(wavid, out fn) ||
                !AudioFileReader.FileExists(b.ToFullPath(fn)))
            {
                // WAV定義がされていなかったか、されていてもファイルが存在しなかった場合（空文字で定義されていた場合を含む）
                return 0L;
            }
            else
            {
                return (new FileInfo(AudioFileReader.FileName(b.ToFullPath(fn)))).Length;
            }
        }

        public void PrepareSynth(int mixChannel, string filename)
        {
            var patch = File.ReadAllText(b.ToFullPath(filename));
            var synth = new HatoSynthDevice(patch);

            MixchToSynth.Add(mixChannel, synth);
        }

        /// <summary>
        /// キー音を　同　期　的　に　読み込みます。
        /// もしキー音がwav/oggであれば、それを読み込みます。
        /// </summary>
        /// <param name="wavid"></param>
        public void PrepareSound(int wavid)
        {
            // TODO: 【重要】WavidToBufferのスレッドセーフ化
            // 参考：https://social.msdn.microsoft.com/Forums/vstudio/ja-JP/1665b757-05ba-4739-9be2-de5ba34f4762/dictionary?forum=csharpgeneralja

            SecondaryBuffer sbuf = null;
            string fn;

            if (!b.WavDefinitionList.TryGetValue(wavid, out fn) ||
                !AudioFileReader.FileExists(b.ToFullPath(fn)))
            {
                // WAV定義がされていなかったか、されていてもファイルが存在しなかった場合（空文字で定義されていた場合を含む）
                lock (WavidToBuffer)
                {
                    WavidToBuffer[wavid] = null;
                    return;
                }
            }
            else
            {
                bool letsread = false;

                lock (WavidToBuffer)
                {
                    if (!WavidToBuffer.TryGetValue(wavid, out sbuf))
                    {
                        // まだ読み込みを開始していなかった場合
                        letsread = true;
                        WavidToBuffer[wavid] = null;  // 同じ音の多重読み込みを防止
                    }
                }

                if (letsread)
                {
                    //await Task.Run(() =>
                    {
                        // TODO:ファイルが壊れていた場合やファイルが未対応形式だった場合になんとかする処理
                        try
                        {
                            sbuf = new SecondaryBuffer(hsound, b.ToFullPath(fn));

                            lock (WavidToBuffer)
                            {
                                WavidToBuffer[wavid] = sbuf;
                            }
                        }
                        catch
                        {
                            // TODO:例外処理の削除
                            //TraceWarning("  Exception: " + e.ToString());
                        }
                    }
                }

                // sbuf にデータが読み込まれた
            }
        }

        /// <summary>
        /// wavidに割り当てられた音を直ちに最初から再生します。
        /// 音声を再生することが出来た場合、trueを返します。（暫定）
        /// </summary>
        /// <param name="wavid">BMSで定義されたwavid</param>
        /// <param name="isKeysound">ユーザーの操作により再生された音であればtrue</param>
        public bool PlaySound(int wavid, bool isKeysound)
        {
            SecondaryBuffer sbuf = null;

            if (WavidToBuffer.TryGetValue(wavid, out sbuf) && sbuf != null)
            {
                sbuf.StopAndPlay(isKeysound ? -6.0 : -10.0);
                return true;
            }
            else
            {
                string fn;
                if (b.WavDefinitionList.TryGetValue(wavid, out fn))
                {
                    // WAV定義はされていたが、wavバッファにwavが読み込まれていない
                    if (fn.ToLower().StartsWith("synth:"))
                    {
                        // シンセ定義だった場合
                        //var match = Regex.Match(fn.Substring(6), @"([0-9]+)\?l([0-9]+)o([0-9]+)[a-gA-G](\+|\-)?");
                        var match = Regex.Match(fn.Substring(6), @"([0-9]+)\?([0-9]+)");
                        if (match.Success)
                        {
                            var mixch = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                            var noteno = Convert.ToInt32(match.Groups[2].Captures[0].Value);

                            if (MixchToSynth.ContainsKey(mixch))
                            {
                                var s = MixchToSynth[mixch];
                                Task.Run(() =>
                                {
                                    lock (MixchToSynth[mixch])
                                    {
                                        MixchToSynth[mixch].NoteOn(noteno);
                                    }
                                });
                                return true;
                            }
                            else
                            {
                                // シンセ定義がただしくされていない場合
                                return false;
                            }
                        }
                        else
                        {
                            // シンセmml命令が構文通りではなかった場合
                            return false;
                        }
                    }
                    else
                    {
                        // WAV定義はされていたが、ファイルが存在しなかったりファイルの読み込みが終了していなかったりした場合
                        // （ファイルが存在しなかった場合を含む）
                        return false;
                    }
                }
                else
                {
                    // WAV定義がされていなかった場合
                    if (isKeysound && DefaultKeySound)
                    {
                        defkey.StopAndPlay(-10.0);
                        return true;  // 暫定
                    }
                    return false;  // 暫定
                }
            }
        }

        SecondaryBuffer synthmix;
        readonly int BufferSamples = 2048;

        Queue<float[]> bufqueue = new Queue<float[]>();
        int bufcount = 0;

        Stopwatch sw = new Stopwatch();

        public void Run()
        {
            synthmix = new SecondaryBuffer(hsound, BufferSamples, 2, 44100);
            sw.Start();

            for (int i = 0; i < 2048; i++)
            {
                bufqueue.Enqueue(new float[] { 0, 0 });
                bufcount++;
            }

            // PreFetch
            Task.Run(async () =>
            {
                while (true)
                {
                    if (bufcount < 2048)
                    {
                        int count = 256;  // 一度に取得しに行くサンプル数 256sample ≒ 5.8ms
                        float[][] buf = new float[2][] { new float[count], new float[count] };

                        foreach (var kvpair in MixchToSynth)
                        {

                            var s = kvpair.Value;
                            float[][] ret = null;

                            lock (s)
                            {
                                ret = s.Take(count).Select(x => x.ToArray()).ToArray();
                            }

                            if (ret.Length == 2)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    buf[0][i] += ret[0][i];
                                    buf[1][i] += ret[1][i];
                                }
                            }
                            else
                            {
                                throw new Exception("フワーッ！");
                            }
                        }

                        lock (bufqueue)
                        {
                            for (int i = 0; i < buf[0].Length; i++)
                            {
                                bufqueue.Enqueue(new float[] { buf[0][i], buf[1][i] });
                            }
                            bufcount += buf[0].Length;
                        }
                    }
                    else
                    {
                        await Task.Delay(5);
                    }
                }
            });

            synthmix.PlayLoop((buf, count) =>
            {
                //Console.WriteLine("required " + count + "samples at " + sw.ElapsedMilliseconds + "ms.");

                if (bufcount < count)
                {
                    Console.WriteLine("Not Enough Buffer... ＞＜");
                }
                else
                {
                    lock (bufqueue)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var sample = bufqueue.Dequeue();
                            buf[0][i] = sample[0];
                            buf[1][i] = sample[1];
                        }
                        bufcount -= count;
                    }
                }
            });
        }
    }
}
