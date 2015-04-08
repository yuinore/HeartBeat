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
        HatoPlayerDevice hplayer;  // FIXME: hplayerはDisposeされているかもしれない
        SecondaryBuffer sbuf;

        public int SamplingRate;
        public int BufSamplesCount;
        public int ChannelsCount;

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
            AudioFileReader.ReadAttribute(filename, out SamplingRate, out ChannelsCount, out BufSamplesCount);

            if (hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf = new SecondaryBuffer(hplayer.hsound, fbuf, BufSamplesCount, ChannelsCount, SamplingRate);

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

        public void StopAndPlay(double volumeInDb = 0)
        {
            amp = (float)Math.Pow(10, volumeInDb * 0.05);

            if (sbuf != null && hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf.StopAndPlay(volumeInDb);
            }
            else
            {
                lock (hplayer.PlayingSoundList)
                {
                    if (!hplayer.PlayingSoundList.Contains(this))
                    {
                        hplayer.PlayingSoundList.Add(this);
                    }
                    playingPosition = 0;
                }
            }
        }
    }
}
