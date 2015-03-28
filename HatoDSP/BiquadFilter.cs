using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class BiquadFilter : Cell
    {
        FilterType type = FilterType.LowPass;

        Cell waveCell;
        Cell cutoffCell;
        IIRFilter filt;

        public BiquadFilter()
        {
        }

        public override void AssignChildren(CellTree[] children)
        {
            waveCell = children[0].Generate();
            if (children.Length >= 2)
            {
                cutoffCell = children[1].Generate();
            }
        }

        public override void AssignControllers(Controller[] ctrl)
        {
            // TODO:
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            Signal[] input = waveCell.Take(count, lenv);
            float[] cutoff = cutoffCell.Take(count, lenv)[0].ToArray();
            filt = filt ?? new IIRFilter(input.Length, 1, 0, 0, 0, 0, 0);

            float[] a0 = new float[count];
            float[] a1 = new float[count];
            float[] a2 = new float[count];
            float[] b0 = new float[count];
            float[] b1 = new float[count];
            float[] b2 = new float[count];

            for (int i = 0; i < count; i++)
            {
                double w0 = 2 * Math.PI * (100 + cutoff[i] * 5000) / lenv.SamplingRate;
                float sin = (float)Math.Sin(w0);
                float cos = (float)Math.Cos(w0);
                float Q = 6.0f;
                float alp = sin / Q;

                switch (type)
                {
                    case FilterType.LowPass:
                        a0[i] = 1 + alp;
                        a1[i] = -2 * cos;
                        a2[i] = 1 - alp;
                        b0[i] = (1 - cos) * 0.5f;
                        b1[i] = 1 - cos;
                        b2[i] = (1 - cos) * 0.5f;
                        break;
                    default:
                        break;
                }
            }

            return filt.Take(count, new Signal[][] { 
                input, 
                new Signal[] { 
                    new ExactSignal(a0,1.0f, false),
                    new ExactSignal(a1,1.0f, false),
                    new ExactSignal(a2,1.0f, false),
                    new ExactSignal(b0,1.0f, false),
                    new ExactSignal(b1,1.0f, false),
                    new ExactSignal(b2,1.0f, false)
                }});
        }
    }
}
