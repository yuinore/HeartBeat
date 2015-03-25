using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public static class FastMath
    {
        static readonly float[] f0;
        static readonly float[] f1;
        static readonly float[] f2;
        static readonly double r;

        private const int N = 512;  // 高速化のために敢えてconstで（高速化になるのか？）
        private const int Mask = N - 1;

        static FastMath()
        {
            f0 = new float[N];
            f1 = new float[N];
            f2 = new float[N];

            double _2pi_N = 2 * Math.PI / N;

            for (int i = 0; i < N; i++)
            {
                f0[i] = (float)Math.Sin(2 * Math.PI * (i + 0.5) / N);
                f1[i] = (float)(_2pi_N * Math.Cos(2 * Math.PI * (i + 0.5) / N));
                f2[i] = -(float)(_2pi_N * _2pi_N * Math.Sin(2 * Math.PI * (i + 0.5) / N) / 2);
            }

            r = (1.0 / _2pi_N);
        }

        public static float Sin(double x)  // 位相は誤差を蓄積させやすいのでdouble型です
        {
            if (x < 0) x = -x;
            double xr = x * r;
            int a = ((int)xr) & Mask;
            double d = xr - (int)xr - 0.5;

            return (float)(f0[a] + d * (f1[a] + d * f2[a]));
        }
    }
}
