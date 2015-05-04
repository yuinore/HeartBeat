using HatoLib;
using HatoSound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoPlayer
{
    /// <summary>
    /// 音声を扱います。HatoPlayerを通じて再生します。
    /// </summary>
    public class Sound : IDisposable
    {
        HatoPlayerDevice hplayer;  // hplayerはDisposeされているかもしれないけどまあ別にいいか

        SecondaryBuffer sbuf;

        public int SamplingRate;
        public int BufSampleCount;
        public int ChannelCount;

        string fn;

        internal float amp = 1.0f;

        internal float[][] fbuf;
        internal double playingPosition = 0;

        public Sound(HatoPlayerDevice hplayer)
        {
            this.hplayer = hplayer;
            sbuf = null;

            lock (hplayer.soundList)
            {
                if (!hplayer.Disposed)
                {
                    hplayer.soundList.Add(this);
                }
                else
                {
                    Dispose();  // コンストラクタからDispose()を呼ぶ人 #いろいろな人
                }
            }
            
        }

        public Sound(HatoPlayerDevice hplayer, string filename)
        {
            // メモ：
            //   1. スレッドAで、Soundのコンストラクタに突入し、重いファイルを読みに行く
            //
            //   2. その後、スレッドBで、HatoPlayerDevice.Dispose() が実行される
            //
            //   3. this (== Sound) のデストラクタが（なぜか）実行される
            //      →この時点では、まだスレッドAで実行されていたコンストラクタは終了していない
            //
            //   4. スレッドAのファイル読み込みが終了する。

            try
            {
                fn = filename;

                this.hplayer = hplayer;

                lock (hplayer.soundList)
                {
                    if (hplayer.Disposed)
                    {
                        Dispose();
                        return;  // disposeされていたら何もしない
                    }
                }

                fbuf = AudioFileReader.ReadAllSamples(filename);  // ここで一度8/16bitから32bitに変換されてしまうんですよね・・・無駄・・・
                AudioFileReader.ReadAttribute(filename, out SamplingRate, out ChannelCount, out BufSampleCount);

                lock (hplayer.soundList)
                {
                    if (!hplayer.Disposed)
                    {
                        if (hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
                        {
                            sbuf = new SecondaryBuffer(hplayer.hsound, fbuf, BufSampleCount, ChannelCount, SamplingRate);

                            fbuf = null;  // DirectSoundモードではバッファはもう不要なので、ガベージコレクタに回収させる（超重要）
                        }

                        hplayer.soundList.Add(this);
                    }
                    else
                    {
                        // やはりDisposeされていたら何もしない
                        Dispose();  // コンストラクタからDispose()を呼ぶ人 #いろいろな人
                    }
                }
            }
            finally
            {
                GC.KeepAlive(this);  // あっても変わらない・・・？
            }
        }

        public void Play()
        {
            amp = 1.0f;

            if (sbuf != null && hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf.Play();
            }
            else
            {
                lock (hplayer.PlayingSoundList)
                {
                    if (!hplayer.PlayingSoundList.Contains(this))
                    {
                        hplayer.PlayingSoundList.Add(this);
                    }
                }
            }
        }

        public void StopAndPlayFrom(double volumeInDb = 0, double playfrom = 0)
        {
            amp = (float)Math.Pow(10, volumeInDb * 0.05);

            if (sbuf != null && hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf.StopAndPlay(volumeInDb, playfrom);
            }
            else
            {
                playingPosition = playfrom * SamplingRate;

                if (playingPosition < BufSampleCount)
                {
                    lock (hplayer.PlayingSoundList)
                    {
                        if (!hplayer.PlayingSoundList.Contains(this))
                        {
                            hplayer.PlayingSoundList.Add(this);
                        }
                    }
                }
            }
        }

        public void StopAndPlay(double volumeInDb = 0)
        {
            StopAndPlayFrom(volumeInDb, 0);
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
            
            // FIXME: 稀にコンストラクタより前に Finalize() が呼ばれることがあるみたい
            if (false)
            {
                System.Diagnostics.Debug.Assert(disposing, "激おこ @ " + this.GetType().ToString());
            }

            if (disposing)
            {
                // Free any other managed objects here.
                if (sbuf != null)
                {
                    sbuf.Dispose();
                }
            }

            // Free any unmanaged objects here.

            disposed = true;
        }
        
        ~Sound()
        {
            Dispose(false);
        }
        #endregion
    }
}
