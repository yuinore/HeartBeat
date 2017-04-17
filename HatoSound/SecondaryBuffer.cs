using HatoLib;
using SharpDX;
using SharpDX.DirectSound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HatoSound
{
    public class SecondaryBuffer : IDisposable
    {
        static readonly object LockObject = new object();
        public int SamplingRate;
        public int BufSampleCount;
        public int ChannelCount;

        int writtenPosition;

        private SecondarySoundBuffer dsSecondaryBuffer;

        /// <summary>
        /// シンセサイザー出力などの連続した音声を出力するバッファを作成します。
        /// </summary>
        /// <param name="hsound"></param>
        /// <param name="bufcount"></param>
        public SecondaryBuffer(HatoSoundDevice hsound, int bufSampleCount, int channelCount = 2, int samplingRate = 44100)
        {
            SamplingRate = samplingRate;
            BufSampleCount = bufSampleCount;
            ChannelCount = channelCount;

            float[][] fbuf = (new[] { 0, 0 }).Select((x) => new float[bufSampleCount]).ToArray();

            CreateBuffer(hsound);
        }
        
        /// <summary>
        /// wav/oggファイルからセカンダリバッファを作成します。ファイルが存在しない場合は例外を投げます。
        /// </summary>
        /// <param name="hsound"></param>
        /// <param name="filename"></param>
        public SecondaryBuffer(HatoSoundDevice hsound, float[][] fbuf, int bufSampleCount, int channelCount = 2, int samplingRate = 44100)
        {
            this.BufSampleCount = bufSampleCount;
            this.ChannelCount = channelCount;
            this.SamplingRate = samplingRate;

            CreateBuffer(hsound);
            WriteSamples(fbuf);
        }

        /// <summary>
        /// 空のバッファを作成します。
        /// </summary>
        public SecondaryBuffer(HatoSoundDevice hsound)
        {
            float[][] fbuf = new float[][] { new float[] { 0 } };
            BufSampleCount = 1;
            ChannelCount = 1;
            SamplingRate = 44100;

            CreateBuffer(hsound);
            WriteSamples(fbuf);
        }

        /// <summary>
        /// コンストラクタから一度だけ呼ばれ、DirectSoundデバイスを作成します。
        /// BufSampleCountなどのフィールドを参照するため、それらを設定してから呼ぶ必要があります。
        /// </summary>
        /// <param name="hsound"></param>
        /// <param name="filename"></param>
        private void CreateBuffer(HatoSoundDevice hsound)
        {
            lock (LockObject)  // これでどう？？？？
            {
                var waveFormat = new SharpDX.Multimedia.WaveFormat(SamplingRate, 16, ChannelCount);

                dsSecondaryBuffer = new SecondarySoundBuffer(hsound.dsound, new SoundBufferDescription()
                {
                    Flags =
                        //BufferFlags.GetCurrentPosition2 |  // ←お前か・・・・・・！！？？？
                        BufferFlags.ControlPositionNotify |
                        BufferFlags.GlobalFocus |
                        BufferFlags.ControlVolume |
                        BufferFlags.StickyFocus,
                    BufferBytes = (BufSampleCount * 16 / 8) * ChannelCount,
                    Format = waveFormat,
                    AlgorithmFor3D = Guid.Empty
                });
            }
        }
        
        /// <summary>
        /// デバイスバッファにデータを書き込みます。
        /// </summary>
        /// <param name="data"></param>
        private void WriteSamples(float[][] data)
        {
            if (data.Length <= 0)
            {
                throw new Exception("オーディオのチャンネル数が0です。");
            }
            WriteSamples(data, 0, data[0].Length);
        }

        /// <summary>
        /// デバイスバッファにデータを書き込みます。
        /// </summary>
        /// <param name="data"></param>
        private void WriteSamples(float[][] data, int dstPositionInSample, int count, bool playing = false)
        {
            // TODO: バッファが短すぎる場合の例外
            short[][] sdata = data.Select(x => x.Select(y =>
            {
                //*** クリッピング処理と、shortへの変換
                double fsample = y * 32767.0;
                short ssample = (short)fsample;
                if (fsample > 32767.0) ssample = 32767;
                if (fsample < -32768.0) ssample = -32768;
                return ssample;
            }).ToArray()).ToArray();  // こ　れ　は　ひ　ど　い

            // Get Capabilties from secondary sound buffer
            var capabilities = dsSecondaryBuffer.Capabilities;

            // Lock the buffer
            DataStream dataPart2;
            var dataPart1 = dsSecondaryBuffer.Lock(
                (dstPositionInSample % BufSampleCount) * ChannelCount * (16 / 8),
                count * ChannelCount * (16 / 8),
                LockFlags.None,
                out dataPart2);

            // Fill the buffer with some sound
            //int numberOfSamples = (int)BufSampleCount * ChannelCount;
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < ChannelCount; j++)
                {
                    if ((dstPositionInSample + i) < BufSampleCount)
                    {
                        dataPart1.Write((short)sdata[j][i]);
                    }
                    else
                    {
                        dataPart2.Write((short)sdata[j][i]);
                    }
                }
            }

            // Unlock the buffer
            dsSecondaryBuffer.Unlock(dataPart1, dataPart2);
        }

        public void Play()
        {
            dsSecondaryBuffer.Volume = 0;
            dsSecondaryBuffer.Play(0, PlayFlags.None);
        }

        public void StopAndPlay(double volumeInDb = 0, double playfrom = 0)
        {
            try
            {
                dsSecondaryBuffer.Stop();
                dsSecondaryBuffer.CurrentPosition = (int)(playfrom * SamplingRate * ChannelCount * sizeof(short));
                dsSecondaryBuffer.Volume = (int)(volumeInDb * 100.0);  // -10000 ～ 0, それぞれ -100dB, 0dB を表す。
                dsSecondaryBuffer.Play(0, PlayFlags.None);
            }
            catch
            {
                // FIXME:
                //   LNの途中で離すとエラー。誰か直して
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventhandler">float[] の0番目から int 個のデータを入れて下さい。</param>
        public void PlayLoop(Action<float[][],int> eventhandler)
        {
            float[][] fbuf = (new float[ChannelCount][]).Select(x => new float[BufSampleCount]).ToArray();

            int playCursor;
            int writeCursor;
            writtenPosition = 0;

            {
                int range = BufSampleCount;

                eventhandler(fbuf, range);

                WriteSamples(fbuf, writtenPosition, range);

                writtenPosition = (writtenPosition + range) % BufSampleCount;
            }

            dsSecondaryBuffer.Play(0, PlayFlags.Looping);

            Task.Run(() =>
            {
                while (true)
                {
                    dsSecondaryBuffer.GetCurrentPosition(out playCursor, out writeCursor);
                    playCursor /= (ChannelCount * 16 / 8);
                    //playCursor = (playCursor - 1000 + BufSampleCount) % BufSampleCount;//適当
                    writeCursor /= (ChannelCount * 16 / 8);

                    //Console.WriteLine("writtenPosition=" + writtenPosition + ", playCursor=" + playCursor + ", writeCursor=" + writeCursor + "; BufSampleCount=" + BufSampleCount);

                    int range = (playCursor - writtenPosition + BufSampleCount) % BufSampleCount;  // ←OK
                    //int range = (writtenPosition - writeCursor + BufSampleCount) % BufSampleCount - 512;

                    //Console.WriteLine(range + " in " + BufSampleCount);

                    //if (range < 0) range = 0;
                    range = Math.Min(range, 256);

                    if (range != 0)
                    {
                        eventhandler(fbuf, range);

                        WriteSamples(fbuf, writtenPosition, range, true);

                        writtenPosition = (writtenPosition + range) % BufSampleCount;
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
            });
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
            
            System.Diagnostics.Debug.Assert(disposing, "激おこ @ " + this.GetType().ToString());

            if (disposing)
            {
                // Free any other managed objects here.
                if (dsSecondaryBuffer != null)
                {
                    dsSecondaryBuffer.Dispose();
                }
            }

            // Free any unmanaged objects here.

            disposed = true;
        }
        
        ~SecondaryBuffer()
        {
            Dispose(false);
        }
        #endregion
    }
}
