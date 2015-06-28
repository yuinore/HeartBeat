using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class AudioSource : InputThroughCell
    {
        readonly string varNameLower;

        public AudioSource()  // 単体テスト用
        {
            varNameLower = "";
        }

        public AudioSource(string sourcename)
        {
            varNameLower = sourcename.ToLower();
        }

        public override int ChannelCountInternal
        {
            get { return 1; }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                };
            }
        }

        public override void TakeInternal(int count, LocalEnvironment lenv)
        {
            Signal sig;

            // TODO: varName が pitch などであった場合の処理
            if (varNameLower == "pitch")
            {
                sig = lenv.Pitch;
            }
            else if (varNameLower == "gate")
            {
                sig = lenv.Gate;
            }
            else if (varNameLower == "freq")
            {
                sig = lenv.Freq;
            }
            else if (varNameLower == "samplingrate")
            {
                sig = new ConstantSignal(lenv.SamplingRate, count);
            }
            else if (lenv.Locals.TryGetValue(varNameLower, out sig))
            {
                // sig is set.
            }
            else
            {
                sig = new ConstantSignal(0, count);
            }

            float[] buf = sig.ToArray();

            for (int i = 0; i < count; i++)
            {
                lenv.Buffer[0][i] += buf[i];
            }
        }
    }
}
