using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class AnalogFilter : Cell
    {
        FilterType type = FilterType.LowPass;

        Cell child;
        IIRFilter filt;

        public AnalogFilter()
        {
        }

        public override void AssignChildren(CellTree children)
        {
            child = children.Generate();
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            Signal[] input = child.Take(count, lenv);
            filt = filt ?? new IIRFilter(input.Length, 1, 0, 0, 0, 0, 0);

            double w0 = 2 * Math.PI * 2000.0 / lenv.SamplingRate;
            float sin = (float)Math.Sin(w0);
            float cos = (float)Math.Cos(w0);
            float Q = 6.0f;
            float alp = sin / Q;

            switch (type)
            {
                case FilterType.LowPass:
                    filt.UpdateParams(1 + alp, -2 * cos, 1 - alp, (1 - cos) * 0.5f, 1 - cos, (1 - cos) * 0.5f);
                    //filt.UpdateParams(1, 0, 0, 1, 0, 0);
                    break;
                default:
                    break;
            }

            return filt.Take(count, new[] { input });
        }
    }
}
