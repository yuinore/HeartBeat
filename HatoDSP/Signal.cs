using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public abstract class Signal  // 不変型にしようかなと考えているのですがどうでしょう
    {
        public static Signal Add(Signal x, Signal y)
        {
            int xcnt = x.Count;
            int ycnt = y.Count;

            if (x is ConstantSignal && y is ConstantSignal && xcnt == ycnt)
            {
                return new ConstantSignal(((ConstantSignal)x).val + ((ConstantSignal)y).val, xcnt);
            }
            else
            {
                // 一般の場合
                float[] ret;

                if (xcnt >= ycnt)
                {
                    ret = x.ToArray();
                    var arr2 = y.ToArray();
                    for (int i = 0; i < ycnt; i++)
                    {
                        ret[i] += arr2[i];
                    }
                }
                else
                {
                    ret = y.ToArray();
                    var arr2 = x.ToArray();
                    for (int i = 0; i < xcnt; i++)
                    {
                        ret[i] += arr2[i];
                    }
                }

                return new ExactSignal(ret, 1.0f, false);
            }
        }

        public static Signal Multiply(Signal x, Signal y)
        {
            // 信号の長さは短い方に合わせられる。これは、信号の末尾に0を外挿した結果です。

            int xcnt = x.Count;
            int ycnt = y.Count;

            if (x is ConstantSignal && xcnt == ycnt)
            {
                if (y is ExactSignal)
                {
                    return new ExactSignal(((ExactSignal)y).array, ((ExactSignal)y).scale * ((ConstantSignal)x).val, true);
                }
            }

            if (y is ConstantSignal && xcnt == ycnt)
            {
                if (x is ExactSignal)
                {
                    return new ExactSignal(((ExactSignal)x).array, ((ExactSignal)x).scale * ((ConstantSignal)y).val, true);
                }
            }

            {
                // 一般の場合
                float[] ret;

                if (xcnt >= ycnt)
                {
                    ret = x.ToArray();
                    var arr2 = y.ToArray();
                    for (int i = 0; i < ycnt; i++)
                    {
                        ret[i] *= arr2[i];
                    }
                }
                else
                {
                    ret = y.ToArray();
                    var arr2 = x.ToArray();
                    for (int i = 0; i < xcnt; i++)
                    {
                        ret[i] *= arr2[i];
                    }
                }

                return new ExactSignal(ret, 1.0f, false);
            }
        }

        public static Signal Concat(Signal x, Signal y)
        {
            return new JoinedSignal(new[] { x, y });
        }

        public static bool Equals(Signal x, Signal y)
        {
            var arr1 = x.ToArray();
            var arr2 = y.ToArray();

            if (arr1.Length != arr2.Length) return false;

            int count = x.Count;

            for (int i = 0; i < count; i++)
            {
                if (arr1[i] != arr2[i]) return false;
            }

            return true;
        }

        public abstract float[] ToArray();

        public abstract int Count
        {
            get;
        }
    }
}
