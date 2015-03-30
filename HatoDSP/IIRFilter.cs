using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class IIRFilter : Module
    {
        readonly int chCnt;

        float a1, a2;
        float b0, b1, b2;
        float[] z0, z1, z2;

        public IIRFilter(int chCnt, float a0, float a1, float a2, float b0, float b1, float b2)
        {
            this.chCnt = chCnt;

            z0 = new float[chCnt];
            z1 = new float[chCnt];
            z2 = new float[chCnt];

            UpdateParams(a0, a1, a2, b0, b1, b2);
        }

        public void UpdateParams(float a0, float a1, float a2, float b0, float b1, float b2)
        {
            float inv_a0 = 1.0f / a0;

            this.a1 = a1 * inv_a0;
            this.a2 = a2 * inv_a0;
            this.b0 = b0 * inv_a0;
            this.b1 = b1 * inv_a0;
            this.b2 = b2 * inv_a0;
        }

        // input[0] : フィルタへの入力信号
        public override Signal[] Take(int count, params Signal[][] input)
        {
            if (input.Length != 1 && input.Length != 2) throw new Exception("Invalid Input Count.");
            if (input[0].Length != chCnt) throw new Exception("Invalid Input Signal's Channels Count.");
            float[][] param = null;
            if (input.Length >= 2)
            {
                param = input[1].Select(x => x.ToArray()).ToArray();
            }

            Signal[] ret = new Signal[input[0].Length];

            for (int j = 0; j < chCnt; j++)
            {
                if (input[0][j].Count != count) throw new Exception("Invalid Input Signal's Length.");

                float[] arr = input[0][j].ToArray();

                float t0 = z0[j];  // これで高速化はされるのか？
                float t1 = z1[j];
                float t2 = z2[j];

                for (int i = 0; i < count; i++)
                {
                    if (input.Length >= 2)
                    {
                        float inv_a0 = 1.0f / param[0][i];
                        a1 = param[1][i] * inv_a0;
                        a2 = param[2][i] * inv_a0;
                        b0 = param[3][i] * inv_a0;
                        b1 = param[4][i] * inv_a0;
                        b2 = param[5][i] * inv_a0;
                    }

                    t0 = arr[i] - a1 * t1 - a2 * t2;
                    arr[i] = t0 * b0 + t1 * b1 + t2 * b2;

                    t2 = t1;
                    t1 = t0;
                }

                ret[j] = new ExactSignal(arr, 1.0f, false);

                z0[j] = t0;
                z1[j] = t1;
                z2[j] = t2;
            }

            return ret;
        }
    }
}
