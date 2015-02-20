using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectSound;
using System.Windows.Forms;

namespace HatoSound
{
    public class HatoSoundPlayer
    {
        internal DirectSound dsound;

        public HatoSoundPlayer(Form form)
        {
            // 参考(SlimDXだけど)
            // http://slimdx.googlecode.com/svn-history/r1737/branches/v2/Samples/DirectSound/PlaySound/Program.cs
            dsound = new DirectSound();
            dsound.SetCooperativeLevel(form.Handle, CooperativeLevel.Priority);  // 優先協調レベルを設定

            var pbuf = new PrimarySoundBuffer(dsound, new SoundBufferDescription()
            {
                Flags = BufferFlags.PrimaryBuffer,
                //BufferBytes = 4096,
                //,
                AlgorithmFor3D = Guid.Empty
            });

            pbuf.Format = new SharpDX.Multimedia.WaveFormat(44100, 16, 2);
        }


    }
}
