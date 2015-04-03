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
    public class HatoPlayerDevice : IDisposable
    {
        //************* 設定 *************
        /// <summary>
        /// キー音の無いオブジェに、デフォルトのキー音を再生するかどうか。
        /// </summary>
        public bool DefaultKeySound = true;

        /// <summary>
        /// 音声を再生するデバイス。DirectSoundとASIOは同時に使用できない（当たり前）
        /// </summary>
        internal PlaybackDeviceType PlaybackDevice = PlaybackDeviceType.ASIO;

        /// <summary>
        /// 音声処理をした結果を格納するバッファのサイズ。
        /// ASIOバッファサイズの2～4倍くらいが良いと思います。
        /// </summary>
        int PrefetchCount = 1024;
        // 512以下だとノイズが出ました。まあASIO 4 AllですしC#ですからそこはあんまり気にしないことにしましょう。
        // 1024でもノイズが出ました。なぜですかね・・・

        //************* デバイス *************
        public BMSStruct b;
        internal HatoSoundDevice hsound;

        //************* データとか *************
        Dictionary<int, Sound> WavidToBuffer = new Dictionary<int, Sound>();
        Dictionary<int, HatoSynthDevice> MixchToSynth = new Dictionary<int, HatoSynthDevice>();

        internal List<Sound> PlayingSoundList = new List<Sound>();

        Sound defkey;

        //************* ここまで *************

        /// <summary>
        /// 音声再生に用いるデバイスを選択します。
        /// </summary>
        public enum PlaybackDeviceType
        {
            DirectSound = 0,
            ASIO
        }

        public HatoPlayerDevice(Form form, BMSStruct b)
        {
            this.b = b;
            hsound = new HatoSoundDevice(form);

            defkey = new Sound(this, HatoPath.FromAppDir("key.ogg"));
        }

        /// <summary>
        /// wavまたはoggファイルが存在した場合にファイルを同期的に読み込みます。
        /// キー音を読み込むには、PrepareSound()の方を使用して下さい。
        /// </summary>
        /// <param name="fullpath">wavまたはoggファイルの、拡張子付きのフルパス。</param>
        /// <returns></returns>
        public Sound LoadAudioFileOrGoEasy(string fullpath)
        {
            if (AudioFileReader.FileExists(fullpath))
            {
                return new Sound(this, fullpath);
            }
            else
            {
                return new Sound(this);
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

            Sound sbuf = null;
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
                            sbuf = new Sound(this, b.ToFullPath(fn));

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
            Sound sbuf = null;

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

        //SecondaryBuffer synthmix;
        AsioHandler asio;
        //readonly int BufferSamples = 2048;

        Queue<float> bufqueueL = new Queue<float>();
        Queue<float> bufqueueR = new Queue<float>();
        int bufcountL = 0;
        int bufcountR = 0;

        int chLeft = 2;
        int chRight = 3;

        private unsafe void AsioCallback(IntPtr buf, int chIdx, int count)
        {
            if (chIdx != chLeft && chIdx != chRight) return;
            //Console.WriteLine("Callback");

            if ((chIdx == chLeft ? bufcountL : bufcountR) < count)
            {
                Console.WriteLine("Not Enough Buffer... ＞＜ " + DateTime.Now.Millisecond);
            }
            else
            {
                lock (bufqueueL)
                {
                    short* p = (short*)buf;
                    for (int i = 0; i < count; i++)
                    {
                        if (chIdx == chLeft)
                        {
                            var sample = bufqueueL.Dequeue();
                            *(p++) = 0;
                            *(p++) = (short)(sample * 32767);
                            bufcountL--;
                        }
                        else if (chIdx == chRight)
                        {
                            var sample = bufqueueR.Dequeue();
                            *(p++) = 0;
                            *(p++) = (short)(sample * 32767);
                            bufcountR--;
                        }
                    }
                }
            }
        }

        public void Run()
        {
            if (PlaybackDevice == PlaybackDeviceType.DirectSound)
            {
                var silence = LoadAudioFileOrGoEasy(HatoPath.FromAppDir("silence20s.wav"));
                silence.StopAndPlay();  // 無音を再生させて、プライマリバッファが稼働していることを保証させる
            }

            if (PlaybackDevice != PlaybackDeviceType.ASIO)
            {
                return;
            }

            asio = new AsioHandler();

            for (int i = 0; i < PrefetchCount; i++)
            {
                bufqueueL.Enqueue(0);
                bufqueueR.Enqueue(0);
                bufcountL++;
                bufcountR++;
            }

            // PreFetch
            Task.Run(async () =>
            {
                while (true)
                {
                    if (bufcountL < PrefetchCount || bufcountR < PrefetchCount)
                    {
                        int count = 256;  // 一度に取得しに行くサンプル数 256sample ≒ 5.8ms

                        float[][] buf = new float[2][] { new float[count], new float[count] };

                        // シンセの再生
                        foreach (var kvpair in MixchToSynth)
                        {
                            var s = kvpair.Value;
                            float[][] ret = null;

                                await Task.Run(() =>
                                {
                                    lock (s)
                                    {
                                        ret = s.Take(count).Select(x => x.ToArray()).ToArray();
                                    }
                                });

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

                        // wavの再生
                        Sound[] slist;
                        lock (PlayingSoundList)
                        {
                            slist = PlayingSoundList.ToArray();
                        }

                        foreach (var snd in slist)
                        {
                            int j = snd.playingPosition;

                            if (snd.ChannelsCount == 1)
                            {
                                for (int i = 0; i < count && j < snd.BufSamplesCount; i++, j++)
                                {
                                    buf[0][i] += snd.fbuf[0][j] * snd.amp;
                                    buf[1][i] += snd.fbuf[0][j] * snd.amp;
                                }
                            }
                            else if (snd.ChannelsCount == 2)
                            {
                                for (int i = 0; i < count && j < snd.BufSamplesCount; i++, j++)
                                {
                                    buf[0][i] += snd.fbuf[0][j] * snd.amp;
                                    buf[1][i] += snd.fbuf[1][j] * snd.amp;
                                }
                            }
                            else
                            {
                                throw new Exception("フワーッ！！！");
                            }
                            snd.playingPosition += count;

                            if (j >= snd.BufSamplesCount)
                            {
                                lock (PlayingSoundList)
                                {
                                    PlayingSoundList.Remove(snd);
                                }
                            }
                        }

                        lock (bufqueueL)
                        {
                            for (int i = 0; i < buf[0].Length; i++)
                            {
                                bufqueueL.Enqueue(buf[0][i]);
                                bufqueueR.Enqueue(buf[1][i]);
                            }
                            bufcountL += buf[0].Length;
                            bufcountR += buf[0].Length;
                        }
                    }
                    else
                    {
                        await Task.Delay(5);
                    }
                }
            });

            asio.Run(AsioCallback);
        }

        #region implementation of IDisposable
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            
            System.Diagnostics.Debug.Assert(disposing, "激おこ");

            if (disposing)
            {
                // Free any other managed objects here.
                if (asio != null)
                {
                    asio.Dispose();
                    asio = null;
                }
            }

            // Free any unmanaged objects here.

            disposed = true;
        }
        
        ~HatoPlayerDevice()
        {
            Dispose(false);
        }
        #endregion
    }
}
