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

        double phase = 0;  // 積分を行うような場合には精度を必要とする

        public AnalogOscillator()
        {
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
                    for (int i = 0; i < count; i++)
                    {
                        double phasedelta = (2 * Math.PI * Math.Pow(2, (pitch[i] - 60.0) / 12.0) * 442 * _1_rate);

                        ret[i] = 0;
                        int n2 = 1;
                        for (double n = phasedelta; n < Math.PI; n += phasedelta)
                        {
                            ret[i] += FastMath.Sin(n2 * phase) * 0.01f / n2++;
                        }
                        phase += phasedelta;
                    }
                    break;

                default:
                    for (int i = 0; i < count; i++)
                    {
                        ret[i] = (float)Math.Sin(phase) * 0.1f;

                        phase += (2 * Math.PI * Math.Pow(2, (pitch[i] - 60.0) / 12.0) * 442 * _1_rate);
                    }
                    break;
            }

            return new[] { new ExactSignal(ret, 1.0f, false) };
        }
    }
}
