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
        static readonly float[] pow2_0;
        static readonly float[] pow2_1;
        static readonly float[] pow2_2;
        static readonly float[][] saw0;
        static readonly float[][] saw1;
        static readonly float[][] saw2;
        static readonly float[][] tri0;
        static readonly float[][] tri1;
        static readonly float[][] tri2;
        static readonly float[][] imp0;
        static readonly float[][] imp1;
        static readonly float[][] imp2;
        static readonly double N_2pi;
 
        // 高速化のために敢えてconstで（高速化になるのか？）
        private const int WT_N = 8;  // wavetable n, sawのwavetableの分割数
        //private const int N_SAW = 256;
        private const int N_SAW = 16;
        private const double INV_2PI = 1.0 / (2 * Math.PI);

        // N = 512 で十分なサイズだと思います（積分しないなら）
        private const int N = 128;
        private const int Mask = N - 1;

        // いくら必要になるまで呼ばれないからといって静的コンストラクタで重い処理をさせるのは良くないと思います。
        static FastMath()
        {

            f0 = new float[N / 2];
            f1 = new float[N / 2];
            f2 = new float[N / 2];
            pow2_0 = new float[N];
            pow2_1 = new float[N];
            pow2_2 = new float[N];

            double _2pi_N = 2 * Math.PI / N;
            double log2_N = Math.Log(2.0) / N;
            N_2pi = (1.0 / _2pi_N);

            for (int i = 0; i < N / 2; i++)
            {
                f0[i] = (float)Math.Sin(2 * Math.PI * (i + 0.5) / N);
                f1[i] = (float)(_2pi_N * Math.Cos(2 * Math.PI * (i + 0.5) / N));
                f2[i] = -(float)(_2pi_N * _2pi_N * Math.Sin(2 * Math.PI * (i + 0.5) / N) / 2);
            }

            for (int i = 0; i < N; i++)
            {
                pow2_0[i] = (float)Math.Pow(2, (i + 0.5) / N);
                pow2_1[i] = (float)(log2_N * Math.Pow(2, (i + 0.5) / N));
                pow2_2[i] = (float)(log2_N * log2_N * Math.Pow(2, (i + 0.5) / N) / 2);
            }

            saw0 = new float[WT_N][];
            saw1 = new float[WT_N][];
            saw2 = new float[WT_N][];
            tri0 = new float[WT_N][];
            tri1 = new float[WT_N][];
            tri2 = new float[WT_N][];
            imp0 = new float[WT_N][];
            imp1 = new float[WT_N][];
            imp2 = new float[WT_N][];

            for (int j = 0; j < WT_N; j++)
            {
                int N2 = N_SAW << j;

                saw0[j] = new float[N2];  // 配列の内容は0で初期化される
                saw1[j] = new float[N2];
                saw2[j] = new float[N2];
                tri0[j] = new float[N2];
                tri1[j] = new float[N2];
                tri2[j] = new float[N2];
                imp0[j] = new float[N2];
                imp1[j] = new float[N2];
                imp2[j] = new float[N2];

                double _2pi_N2 = 2 * Math.PI / N2;

                for (int i = 0; i < N2; i++)
                {
                    for (int n = 1; n <= 1 << j; n++)
                    {
                        saw0[j][i] += (float)(Math.Sin(2 * Math.PI * n * (i + 0.5) / N2) / (n * Math.PI / 2));
                        saw1[j][i] += (float)(_2pi_N2 * n * Math.Cos(2 * Math.PI * n * (i + 0.5) / N2) / (n * Math.PI / 2));
                        saw2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math.Sin(2 * Math.PI * n * (i + 0.5) / N2) / (2 * n * Math.PI / 2));

                        imp0[j][i] += (float)(Math.Cos(2 * Math.PI * n * (i + 0.5) / N2) / (Math.PI / 2));
                        imp1[j][i] += (float)(-_2pi_N2 * n * Math.Sin(2 * Math.PI * n * (i + 0.5) / N2) / (Math.PI / 2));
                        imp2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math.Cos(2 * Math.PI * n * (i + 0.5) / N2) / (2 * Math.PI / 2));
                        
                        if (n % 2 == 1)
                        {
                            tri0[j][i] += (float)(Math.Cos(2 * Math.PI * n * (i + 0.5) / N2) / (n * n * Math.PI * Math.PI / 8));
                            tri1[j][i] += (float)(-_2pi_N2 * n * Math.Sin(2 * Math.PI * n * (i + 0.5) / N2) / (n * n * Math.PI * Math.PI / 8));
                            tri2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math.Cos(2 * Math.PI * n * (i + 0.5) / N2) / (2 * n * n * Math.PI * Math.PI / 8));
                        }
                    }
                }
            }
        }

        public static double Sin(double x)
        {
            if (x < 0) x = -x;
            double xr = x * N_2pi;
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

        public static double Pow2(double x)
        {
            //int integPart = (int)(x - ((int)x - 1)) + ((int)x - 1);  // floor(x) ← おまけ

            if (x >= 0)
            {
                double absx = x;
                int integPart = (int)absx;  // floor(x)
                double xr = (absx - integPart) * N;
                int a = (int)xr;  // 0 ～ N-1

                double d = xr - a - 0.5;  // テイラー展開の基準点からの差

                return (pow2_0[a] + d * (pow2_1[a] + d * pow2_2[a])) * (1 << integPart);
            }
            else
            {
                double absx = -x;
                int integPart = (int)absx;  // floor(x)
                double xr = (absx - integPart) * N;
                int a = (int)xr;  // 0 ～ N-1

                double d = xr - a - 0.5;  // テイラー展開の基準点からの差

                return 1.0f / ((pow2_0[a] + d * (pow2_1[a] + d * pow2_2[a])) * (1 << integPart));  // 逆数を返す（ちょっと遅い）
            }
        }

        public static double Saw(double x, int logovertone)
        {
            if (logovertone >= WT_N) logovertone = WT_N - 1;
            if (logovertone < 0) logovertone = 0;

            int N2 = N_SAW << logovertone;
            int mask = N2 - 1;

            if (x < 0) x = -x;  // Fixme:
            double xr = x * N2 * INV_2PI;
            int a = ((int)xr) & mask;
            double d = xr - (int)xr - 0.5;

            return saw0[logovertone][a] + d * (saw1[logovertone][a] + d * saw2[logovertone][a]);
        }

        public static double Tri(double x, int logovertone)
        {
            if (logovertone >= WT_N) logovertone = WT_N - 1;
            if (logovertone < 0) logovertone = 0;

            int N2 = N_SAW << logovertone;
            int mask = N2 - 1;

            if (x < 0) x = -x;  // Fixme:
            double xr = x * N2 * INV_2PI;
            int a = ((int)xr) & mask;
            double d = xr - (int)xr - 0.5;

            return tri0[logovertone][a] + d * (tri1[logovertone][a] + d * tri2[logovertone][a]);
        }

        public static double Impulse(double x, int logovertone)
        {
            if (logovertone >= WT_N) logovertone = WT_N - 1;
            if (logovertone < 0) logovertone = 0;

            int N2 = N_SAW << logovertone;
            int mask = N2 - 1;

            if (x < 0) x = -x;  // Fixme:
            double xr = x * N2 * INV_2PI;  // TODO:除算の削除
            int a = ((int)xr) & mask;
            double d = xr - (int)xr - 0.5;

            return imp0[logovertone][a] + d * (imp1[logovertone][a] + d * imp2[logovertone][a]);
        }
    }
}
