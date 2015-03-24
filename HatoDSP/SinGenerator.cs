using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class SinGenerator : Module
    {
        int index = 0;

        public override Signal[] Take(int count, Signal[][] input)
        {
            float[] ret = new float[count];

            for (int i = 0; i < count; i++)
            {
                ret[i] = (float)Math.Sin(2 * Math.PI * 441 * index++ / 44100) * 0.1f;
            }

            return new[] { new ExactSignal(ret, 1.0f, false) };
        }
    }
}
