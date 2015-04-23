using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public abstract class Signal  // 不変型という前提の最適化処理を既にいくつか実装していますのでそういうことにしておいてください。もっとも、建前上不変型なだけですが。
    {
        public static Signal Add(Signal x, Signal y)
        {
            int xcnt = x.Count;
            int ycnt = y.Count;

            System.Diagnostics.Debug.Assert(xcnt == ycnt, "長さの異なるSignalの和を計算しようとしました。");

            if (xcnt == ycnt)
            {
                if (x is ConstantSignal && y is ConstantSignal)
                {
                    return new ConstantSignal(((ConstantSignal)x).val + ((ConstantSignal)y).val, xcnt);
                }
                else if (x is ConstantSignal)
                {
                    if (((ConstantSignal)x).val == 0)
                    {
                        return y;
                    }
                    else
                    {
                        float[] ret = y.ToArray();
                        float c = ((ConstantSignal)x).val;

                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] += c;
                        }

                        return new ExactSignal(ret, 1.0f, false);
                    }
                }
                else if (y is ConstantSignal)
                {
                    if (((ConstantSignal)y).val == 0)
                    {
                        return x;
                    }
                    else
                    {
                        float[] ret = x.ToArray();
                        float c = ((ConstantSignal)y).val;

                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] += c;
                        }

                        return new ExactSignal(ret, 1.0f, false);
                    }
                }
                else if (x is ExactSignal && y is ExactSignal)
                {
                    float[] ret = x.ToArray();
                    var arr2 = ((ExactSignal)y).array;
                    var scale = ((ExactSignal)y).scale;

                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] += arr2[i] * scale;
                    }

                    return new ExactSignal(ret, 1.0f, false);
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

        internal static Signal AddRange(Signal[] signals)
        {
            if (signals.Length <= 1)
            {
                if (signals.Length == 0)
                {
                    //return new ConstantSignal(0f, 0);
                    throw new Exception("signalの長さが不明です。");
                }
                if (signals.Length == 1) return signals[0];
            }

            float[] buf = null;
            float c = 0f;  // 定数分
            int count = signals[0].Count;

            foreach (var x in signals)
            {
                if (count != x.Count) throw new Exception("長さの異なるSignalの和を計算しようとしました。");

                if (x is ConstantSignal)
                {
                    c += ((ConstantSignal)x).val;
                }
                else if (x is ExactSignal)
                {
                    if (buf == null)
                    {
                        buf = x.ToArray();
                    }
                    else
                    {
                        var arr2 = ((ExactSignal)x).array;
                        var scale = ((ExactSignal)x).scale;

                        for (int i = 0; i < buf.Length; i++)
                        {
                            buf[i] += arr2[i] * scale;
                        }
                    }
                }
                else
                {
                    // 一般の場合
                    if (buf == null)
                    {
                        buf = x.ToArray();
                    }
                    else
                    {
                        var arr2 = x.ToArray();
                        for (int i = 0; i < buf.Length; i++)
                        {
                            buf[i] += arr2[i];
                        }
                    }
                }
            }

            if (buf == null)
            {
                return new ConstantSignal(c, count);  // すべて定数だった
            }

            if (c != 0)
            {
                for (int i = 0; i < buf.Length; i++)
                {
                    buf[i] += c;
                }
            }

            return new ExactSignal(buf, 1.0f, false);
        }

        public static Signal Multiply(Signal x, Signal y)
        {
            // 信号の長さは短い方に合わせられる。これは、信号の末尾に0を外挿した結果です。(ほんとかな？？)
            // と思ったのですが、長さの違う信号同士を掛けることがシンセを作る過程であまり無さそうなので、
            // 信号の長さは同じという前提の最適化処理をしても良さそう

            int xcnt = x.Count;
            int ycnt = y.Count;

            System.Diagnostics.Debug.Assert(xcnt == ycnt, "長さの異なるSignalの積を計算しようとしました。");

            if (xcnt == ycnt)
            {
                if (x is ConstantSignal)
                {
                    if (x is ConstantSignal && y is ConstantSignal)
                    {
                        return new ConstantSignal(((ConstantSignal)x).val * ((ConstantSignal)y).val, xcnt);
                    }
                    else if (y is ExactSignal)
                    {
                        return new ExactSignal(((ExactSignal)y).array, ((ExactSignal)y).scale * ((ConstantSignal)x).val, true);
                    }
                    else if (((ConstantSignal)x).val == 1)
                    {
                        return y;
                    }
                    else if (((ConstantSignal)x).val == 0)
                    {
                        return new ConstantSignal(0, ycnt);
                    }
                }

                if (y is ConstantSignal)
                {
                    if (x is ExactSignal)
                    {
                        return new ExactSignal(((ExactSignal)x).array, ((ExactSignal)x).scale * ((ConstantSignal)y).val, true);
                    }
                    else if (((ConstantSignal)y).val == 1)
                    {
                        return x;
                    }
                    else if (((ConstantSignal)y).val == 0)
                    {
                        return new ConstantSignal(0, xcnt);
                    }
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
