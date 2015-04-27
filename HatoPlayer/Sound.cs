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
    public class Sound
    {
        HatoPlayerDevice hplayer;  // hplayerはDisposeされているかもしれないけどまあ別にいいか
        SecondaryBuffer sbuf;

        public int SamplingRate;
        public int BufSampleCount;
        public int ChannelCount;

        internal float amp = 1.0f;

        internal float[][] fbuf;
        internal double playingPosition = 0;

        public Sound(HatoPlayerDevice hplayer)
        {
            this.hplayer = hplayer;
            sbuf = null;
        }

        public Sound(HatoPlayerDevice hplayer, string filename)
        {
            this.hplayer = hplayer;

            // ↓ここで同時にNVorbisからの2ファイルの読み込みが発生しているのかもしれない
            fbuf = AudioFileReader.ReadAllSamples(filename);  // ここで一度8/16bitから32bitに変換されてしまうんですよね・・・無駄・・・
            AudioFileReader.ReadAttribute(filename, out SamplingRate, out ChannelCount, out BufSampleCount);

            if (hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf = new SecondaryBuffer(hplayer.hsound, fbuf, BufSampleCount, ChannelCount, SamplingRate);

                fbuf = null;  // ガベージコレクタに回収させる（超重要）
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
                if (playfrom < 0.01)
                {
                    sbuf.StopAndPlay(volumeInDb);
                }
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
    }
}
