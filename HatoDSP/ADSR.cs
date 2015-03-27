using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class ADSR : Cell
    {
        CellTree child0;

        Waveform waveform = Waveform.Saw;

        double time = 0;
        int n = 0;

        float A = 0.01f;
        float D = 0.5f;
        float S = 0.1f;
        float R = 0.01f;


        public ADSR()
        {
        }

        public override void AssignChildren(CellTree[] children)
        {
            this.child0 = children[0];
        }

        public override void AssignControllers(Controller[] ctrl)
        {
            // TODO:
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            float[] ret = new float[count];

            double dt = 1.0 / lenv.SamplingRate;

            for (int i = 0; n < count; n++, i++)
            {
                if (time < A)
                {
                    ret[i] = (float)(time / A);
                    time += dt;
                }
                else if (time < A + D)
                {
                    ret[i] = (float)(1.0f - (1.0f - S) * (time - A) / D);
                    time += dt;
                }
                else
                {
                    ret[i] = S;
                }
                // TODO: リリースの実装
            }

            return new[] { new ExactSignal(ret, 1.0f, false) };
        }
    }
}
