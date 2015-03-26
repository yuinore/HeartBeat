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
        static readonly float[][] saw0;
        static readonly float[][] saw1;
        static readonly float[][] saw2;
        static readonly double r;

        private const int WT_N = 8;  // wavetable n
        private const int N_SAW = 128;  // 高速化のために敢えてconstで（高速化になるのか？）
        private const double INV_2PI = 1.0 / (2 * Math.PI);

        // N = 512 で十分なサイズだと思います（積分しないなら）
        private const int N = 512;  // 高速化のために敢えてconstで（高速化になるのか？）
        private const int Mask = N - 1;

        static FastMath()
        {
            f0 = new float[N / 2];
            f1 = new float[N / 2];
            f2 = new float[N / 2];

            double _2pi_N = 2 * Math.PI / N;

            for (int i = 0; i < N / 2; i++)
            {
                f0[i] = (float)Math.Sin(2 * Math.PI * (i + 0.5) / N);
                f1[i] = (float)(_2pi_N * Math.Cos(2 * Math.PI * (i + 0.5) / N));
                f2[i] = -(float)(_2pi_N * _2pi_N * Math.Sin(2 * Math.PI * (i + 0.5) / N) / 2);
            }

            saw0 = new float[WT_N][];
            saw1 = new float[WT_N][];
            saw2 = new float[WT_N][];

            for (int j = 0; j < WT_N; j++)
            {
                int N2 = N_SAW << j;

                saw0[j] = new float[N2];  // 配列の内容は0で初期化される
                saw1[j] = new float[N2];
                saw2[j] = new float[N2];

                double _2pi_N2 = 2 * Math.PI / N2;

                for (int i = 0; i < N2; i++)
                {
                    for (int n = 1; n <= 1 << j; n++)
                    {
                        saw0[j][i] += (float)Math.Sin(2 * Math.PI * n * (i + 0.5) / N2) / n;
                        saw1[j][i] += (float)(_2pi_N2 * n * Math.Cos(2 * Math.PI * (i + 0.5) / N2)) / n;
                        saw2[j][i] += -(float)(_2pi_N2 * _2pi_N2 * n * n * Math.Sin(2 * Math.PI * (i + 0.5) / N2) / 2) / n;
                    }
                }
            }

            r = (1.0 / _2pi_N);
        }

        public static double Sin(double x)  // 位相は誤差を蓄積させやすいのでdouble型です。というかfloatよりdoubleの方が速いというのは本当みたいです。
        {
            if (x < 0) x = -x;
            double xr = x * r;
            int a = ((int)xr) & Mask;
            if (a < N / 2)
            {
                double d = xr - (int)xr - 0.5;

                return f0[a] + d * (f1[a] + d * f2[a]);
            }
            else
            {
                a = a & (N / 2 - 1);

                double d = xr - (int)xr - 0.5;

                return -(f0[a] + d * (f1[a] + d * f2[a]));
            }
        }

        public static double Saw(double x, int logovertone)  // 位相は誤差を蓄積させやすいのでdouble型です。というかfloatよりdoubleの方が速いというのは本当みたいです。
        {
            if (logovertone >= WT_N) logovertone = WT_N - 1;

            int N2 = N_SAW << logovertone;
            int mask = N2 - 1;

            if (x < 0) x = -x;  // Fixme:
            double xr = x * N2 * INV_2PI;  // TODO:除算の削除
            int a = ((int)xr) & mask;
            double d = xr - (int)xr - 0.5;

            return saw0[logovertone][a] + d * (saw1[logovertone][a] + d * saw2[logovertone][a]);
        }
    }
}
