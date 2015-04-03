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
        HatoPlayerDevice hplayer;
        SecondaryBuffer sbuf;

        public Sound(HatoPlayerDevice hplayer)
        {
            this.hplayer = hplayer;
            sbuf = null;
        }

        public Sound(HatoPlayerDevice hplayer, string filename)
        {
            this.hplayer = hplayer;

            if (hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf = new SecondaryBuffer(hplayer.hsound, filename);
            }
        }

        public void Play()
        {
            if (sbuf != null && hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf.Play();
            }
        }

        public void StopAndPlay(double volumeInDb = 0)
        {
            if (sbuf != null && hplayer.PlaybackDevice == HatoPlayerDevice.PlaybackDeviceType.DirectSound)
            {
                sbuf.StopAndPlay(volumeInDb);
            }
        }
    }
}
