using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class AnalogOscillator : Cell
    {
        CellTree children;

        Waveform waveform = Waveform.Saw;

        const int MAX_OVERTONE = 100;
        float[] int_inv;
        double old = 0;
        int i2 = 0;

        double phase = 0;  // 積分を行うような場合には精度を必要とする

        public AnalogOscillator()
        {
            int_inv = Enumerable.Range(0, MAX_OVERTONE).Select(x => x == 0 ? 1f : (float)(1.0 / x)).ToArray();
        }

        public override void AssignChildren(CellTree[] children)
        {
            this.children = children[0];
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            float[] ret = new float[count];

            float[] pitch = lenv.Pitch.ToArray();  // TODO:ConstantSignalの場合の最適化

            float _1_rate = 1.0f / lenv.SamplingRate;

            switch (waveform)
            {
                case Waveform.Saw:
                    for (int i = 0; i < count; i++, i2++)
                    {
                        double freq = Math.Pow(2, (pitch[i] - 60.0) / 12.0) * 600;
                        double phasedelta = (2 * Math.PI * freq * _1_rate);

                        /*
                        ret[i] = 0;
                        int n2 = 1;
                        for (double n = phasedelta; n < Math.PI && n2 <= MAX_OVERTONE; n += phasedelta, n2 ++)
                        {
                            ret[i] += (float)(FastMath.Sin(n2 * phase) * 0.01 * int_inv[n2] * int_inv[n2]);
                        }*/
                        double P = lenv.SamplingRate / freq;
                        double M = Math.Floor((P + 1) / 2) * 2 - 1;
                        old = old + FastMath.Sin(Math.PI * M / P * (i2 + 1e-1)) / (FastMath.Sin(Math.PI / P * (i2 + 1e-1)) * P) - 1 / P;  // TODO:ゼロ除算
                        ret[i] = (float)((old - 0.5) * 0.01);
                        //ret[i] += (float)((0.5 - (phase / (2 * Math.PI)) % 1) * 0.01);
                        phase += phasedelta;
                    }
                    break;

                default:
                    for (int i = 0; i < count; i++)
                    {
                        ret[i] = (float)(FastMath.Sin(phase) * 0.1);

                        phase += (2 * Math.PI * Math.Pow(2, (pitch[i] - 60.0) / 12.0) * 442 * _1_rate);
                    }
                    break;
            }

            return new[] { new ExactSignal(ret, 1.0f, false) };
        }
    }
}
