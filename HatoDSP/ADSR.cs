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
        CellParameterValue[] ctrl;

        double time = 0;  // 累積時間
        int n = 0;  // 累積サンプル数

        float A = 0.00f;
        float D = 0.5f;
        float S = 0.1f;
        float R = 0.01f;

        float lastgain = 0.0f;
        double releasedAt = 0.0f;
        bool releaseFinished = false;

        public ADSR()
        {
        }

        public override CellParameter[] ParamsList
        {
            get
            {
                // TODO: パラメータの対数化
                return new CellParameter[]{
                    new CellParameter("Attack", true, 0, 1, 0.01f, x => x + "s"),
                    new CellParameter("Decay", true, 0, 1, 0.5f, x => x + "s"),
                    new CellParameter("Sustain", true, 0, 1, 0.1f, x => x + "s"),
                    new CellParameter("Release", true, 0, 1, 0.01f, x => x + "s")
                };
            }
        }

        public override void AssignChildren(CellTree[] children)
        {
            this.child0 = children[0];
            cell = child0.Generate();
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            this.ctrl = ctrl;
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            if (releaseFinished) return new Signal []{ new ConstantSignal(0, count) };

            // ctrlの解釈
            // A, D, S, R
            if (ctrl != null)
            {
                if (ctrl.Length >= 1) { A = ctrl[0].Value; }
                if (ctrl.Length >= 2) { D = ctrl[1].Value; }
                if (ctrl.Length >= 3) { S = ctrl[2].Value; }
                if (ctrl.Length >= 4) { R = ctrl[3].Value; }
            }

            float[] gate = lenv.Gate.ToArray();

            float[] ret = new float[count];

            double dt = 1.0 / lenv.SamplingRate;

            double log2a = Math.Log(0.00001, 2);  // -100dB (1 / 10^5)

            for (int i = 0; i < count; n++, i++, time += dt)
            {
                if (gate[i] > 0.5)
                {
                    if (time < A)
                    {
                        lastgain = ret[i] = (float)(time / A);
                    }
                    else if (time < A + D)
                    {
                        //double rate = Math.Pow(0.00001, (time - A) / D);
                        double rate = FastMath.Pow2(log2a * (time - A) / D); // 0dB to -100dB
                        lastgain = ret[i] = (float)((1.0f - S) * rate + S);
                    }
                    else
                    {
                        lastgain = ret[i] = S;  // TODO: ConstantSignal化
                    }
                    releasedAt = time;
                }
                else
                {
                    if (time < releasedAt + R)
                    {
                        //double rate = Math.Pow(0.00001, (time - releasedAt) / R);
                        double rate = FastMath.Pow2(log2a * (time - releasedAt) / R); // 0dB to -100dB
                        ret[i] = (float)(lastgain * rate);
                    }
                    else
                    {
                        ret[i] = 0;  // TODO: ConstantSignal化
                        releaseFinished = true;
                    }
                }

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
