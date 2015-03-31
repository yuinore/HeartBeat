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
        Cell cell;
        Controller[] ctrl;

        Waveform waveform = Waveform.Saw;

        double time = 0;
        int n = 0;

        float A = 0.00f;
        float D = 0.5f;
        float S = 0.1f;
        float R = 0.01f;


        public ADSR()
        {
        }

        public override void AssignChildren(CellTree[] children)
        {
            this.child0 = children[0];
            cell = child0.Generate();
        }

        public override void AssignControllers(Controller[] ctrl)
        {
            this.ctrl = ctrl;
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            // ctrlの解釈
            // A, D, S, R
            if (ctrl != null)
            {
                if (ctrl.Length >= 1) { A = ctrl[0].Value; }
                if (ctrl.Length >= 2) { D = ctrl[1].Value; }
                if (ctrl.Length >= 3) { S = ctrl[2].Value; }
                if (ctrl.Length >= 4) { R = ctrl[3].Value; }
            }

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

            if (child0 != null)
            {
                var src = cell.Take(count, lenv);
                return src.Select(x => Signal.Multiply(x, new ExactSignal(ret, 1.0f, false))).ToArray();  // チャンネル数は入力信号と同じ
            }
            else
            {
                return new[] { new ExactSignal(ret, 1.0f, false) };  // チャンネル数は1
            }
        }
    }
}
