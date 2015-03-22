using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class JoinedSignal : Signal
    {
        Signal[] signals;
        int totalcount;

        public JoinedSignal(Signal[] signals)
        {
            this.signals = signals;
            totalcount = 0;

            for (int i = 0; i < signals.Length; i++)
            {
                totalcount += signals[i].Count;
            }
        }

        public override float[] ToArray()
        {
            float[] ret = new float[totalcount];
            int index = 0;

            for (int i = 0; i < signals.Length; i++)
            {
                var arr = signals[i].ToArray();
                Array.Copy(arr, 0, ret, index, arr.Length);
                index += arr.Length;
            }

            return ret;
        }

        public override int Count
        {
            get { return totalcount; }
        }
    }
}
