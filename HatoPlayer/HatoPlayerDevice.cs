﻿using HatoBMSLib;
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
        /// シンセサイザーの音量。キー音とそれ以外で分けられたら良いのですが。
        /// </summary>
        float SynthVolumeInDb = -10.0f;

        /// <summary>
        /// 音声処理をした結果を格納するバッファのサイズ。
        /// ASIOバッファサイズの2～4倍くらいが良いと思います。
        /// </summary>
        int PrefetchCount = 2048;
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

            if (PlaybackDevice == PlaybackDeviceType.DirectSound)
            {
                hsound = new HatoSoundDevice(form);
            }

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
        public bool PlaySound(int wavid, bool isKeysound, double playfrom)
        {
            Sound sbuf = null;

            if (WavidToBuffer.TryGetValue(wavid, out sbuf) && sbuf != null)
            {
                sbuf.StopAndPlayFrom(isKeysound ? -6.0 : -10.0, playfrom);
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
                        var match = Regex.Match(fn.Substring(6), @"([0-9]+)\?(.*)");
                        if (match.Success)
                        {
                            int mixch = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                            string query = match.Groups[2].Captures[0].Value;
                            try
                            {
                                HatoLib.Midi.MidiEventNote mev = HatoLib.Midi.MidiEventNote.NewFromQuery(query, 15360);
                                int noteno = mev.n;
                                double duration = (mev.q / 15360.0) / (112.0 / 60.0);

                                if (MixchToSynth.ContainsKey(mixch))
                                {
                                    var s = MixchToSynth[mixch];
                                    Task.Run(async () =>
                                    {
                                        MixchToSynth[mixch].NoteOn(noteno);  // FIXME: playfromに応じて途中再生？

                                        await Task.Delay((int)(duration * 1000));

                                        MixchToSynth[mixch].NoteOff(noteno);  // TODO: もうちょっと真面目にノートオン/オフを書く
                                    });
                                    return true;
                                }
                                else
                                {
                                    // シンセ定義が正しくされていない場合
                                    return false;
                                }
                            }
                            catch
                            {
                                // シンセmml命令が構文通りではなかった場合
                                Debug.Assert(false);
                            }
                            return false;
                        }
                        else
                        {
                            // synth: 定義が構文通りではなかった場合
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

        Queue<float> bufqueueL = new Queue<float>();  // lock にはすべて bufqueueL のみを用いる
        Queue<float> bufqueueR = new Queue<float>();
        int bufcountL = 0;
        int bufcountR = 0;

        private unsafe void AsioCallback(AsioHandler.AsioBuffer aBuf)
        {
            if (bufcountL < aBuf.SampleCount || bufcountR < aBuf.SampleCount)
            {
                Console.WriteLine("Not Enough Buffer... ＞＜ " + DateTime.Now.Millisecond);
            }
            else
            {
                lock (bufqueueL)
                {
                    for (int i = 0; i < aBuf.SampleCount; i++)
                    {
                        aBuf.Buffer[0][i] = bufqueueL.Dequeue();
                        bufcountL--;
                        aBuf.Buffer[1][i] = bufqueueR.Dequeue();
                        bufcountR--;
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
                        float amp = (float)Math.Pow(10, SynthVolumeInDb * 0.05);

                        foreach (var kvpair in MixchToSynth)
                        {
                            var s = kvpair.Value;
                            float[][] ret = null;

                            await Task.Run(() =>
                            {
                                ret = s.Take(count).Select(x => x.ToArray()).ToArray();
                            });

                            if (ret.Length == 2)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    buf[0][i] += ret[0][i] * amp;
                                    buf[1][i] += ret[1][i] * amp;
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
                            int j = 0;

                            if (snd.SamplingRate == 44100)
                            {
                                j = (int)snd.playingPosition;

                                if (snd.ChannelCount == 1)
                                {
                                    for (int i = 0; i < count && j < snd.BufSampleCount; i++, j++)
                                    {
                                        buf[0][i] += snd.fbuf[0][j] * snd.amp;
                                        buf[1][i] += snd.fbuf[0][j] * snd.amp;
                                    }
                                }
                                else if (snd.ChannelCount == 2)
                                {
                                    for (int i = 0; i < count && j < snd.BufSampleCount; i++, j++)
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
                            }
                            else
                            {
                                double jd = snd.playingPosition;

                                if (snd.ChannelCount == 1)
                                {
                                    for (int i = 0; i < count && jd < snd.BufSampleCount - 1; i++)
                                    {
                                        int j0 = (int)jd;
                                        float t = (float)(jd - j0);

                                        buf[0][i] += ((1 - t) * snd.fbuf[0][j0] + t * snd.fbuf[0][j0 + 1]) * snd.amp;
                                        buf[1][i] += ((1 - t) * snd.fbuf[0][j0] + t * snd.fbuf[0][j0 + 1]) * snd.amp;

                                        jd += snd.SamplingRate / 44100.0;
                                    }
                                }
                                else if (snd.ChannelCount == 2)
                                {
                                    for (int i = 0; i < count && jd < snd.BufSampleCount - 1; i++)
                                    {
                                        int j0 = (int)jd;
                                        float t = (float)(jd - j0);

                                        buf[0][i] += ((1 - t) * snd.fbuf[0][j0] + t * snd.fbuf[0][j0 + 1]) * snd.amp;
                                        buf[1][i] += ((1 - t) * snd.fbuf[1][j0] + t * snd.fbuf[1][j0 + 1]) * snd.amp;

                                        jd += snd.SamplingRate / 44100.0;
                                    }
                                }
                                else
                                {
                                    throw new Exception("フワーッ！！！");
                                }

                                j = (int)jd + 1;

                                snd.playingPosition += count * snd.SamplingRate / 44100.0;
                            }

                            if (j >= snd.BufSampleCount)
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
                        await Task.Delay(1);  // 一度Delayすると、15ミリ秒くらいは処理が返ってこないらしい・・・
                        //Thread.Sleep(1);  // 新しくスレッドを作成したほうが、11ミリ秒くらいになり、若干マシな気がします・・・。
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
