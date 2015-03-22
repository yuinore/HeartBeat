using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class ConstantSignal : Signal
    {
        internal readonly float val;
        internal readonly int count;

        public ConstantSignal(float val, int count)
        {
            this.val = val;
            this.count = count;
        }

        public override float[] ToArray() {
            float[] ret = new float[count];

            for (int i = 0; i < count; i++)
            {
                ret[i] = val;
            }

            return ret;
        }

        public override int Count
        {
            get { return count; }
        }
    }
}
