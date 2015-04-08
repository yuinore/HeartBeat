#include "stdafx.h"
#include "FastMath.h"
#include <stdlib.h>
#include <string.h>

namespace HatoDSPFast {
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace System::Text;
    using namespace System::Threading::Tasks;

    // いくら必要になるまで呼ばれないからといって静的コンストラクタで重い処理をさせるのは良くないと思います。
    static FastMath::FastMath()
    {
        // デリゲートとメソッドグループ（メソッド定義）の違いとは
        Task::Factory->StartNew(gcnew Action(Initialize));

        // Delegateの細かいこと - 奇想曲 in C# - はてなダイアリー
        // http://d.hatena.ne.jp/toshi_m/20100717/1279368502
    }

    void FastMath::Initialize() {
        WT_SIZE = (int*)calloc(WT_N, sizeof(int));
        WT_SIZE_2PI = (double*)calloc(WT_N, sizeof(double));
        WT_MASK = (int*)calloc(WT_N, sizeof(int));

        int temp[WT_N] = { 8, 512, 512, 512, 1024, 1024, 2048, 2048, 4096, 4096 };  // あえてわかりやすくするために temp[0]=8 で
        // N_SAW換算で   { 8, 256, 128, 64,  64,   32,   32,   16,   16,   8 };  // 最高周波数の倍音1周期あたりに割かれるサンプル数

        for (int j = 0; j < WT_N; j++) {
            WT_SIZE[j] = temp[j];
            WT_SIZE_2PI[j] = temp[j] / (2.0 * Math::PI);
            WT_MASK[j] = temp[j] - 1;
        }

        f0 = (float*)calloc(N / 2, sizeof(float));
        f1 = (float*)calloc(N / 2, sizeof(float));
        f2 = (float*)calloc(N / 2, sizeof(float));
        pow2_0 = (float*)calloc(N, sizeof(float));
        pow2_1 = (float*)calloc(N, sizeof(float));
        pow2_2 = (float*)calloc(N, sizeof(float));
        ipw2_0 = (float*)calloc(N, sizeof(float));
        ipw2_1 = (float*)calloc(N, sizeof(float));
        ipw2_2 = (float*)calloc(N, sizeof(float));

        for (int i = 0; i < N / 2; i++)
        {
            f0[i] = (float)Math::Sin(2 * Math::PI * (i + 0.5) / N);
            f1[i] = (float)(_2pi_N * Math::Cos(2 * Math::PI * (i + 0.5) / N));
            f2[i] = -(float)(_2pi_N * _2pi_N * Math::Sin(2 * Math::PI * (i + 0.5) / N) / 2);
        }

        for (int i = 0; i < N; i++)
        {
            pow2_0[i] = (float)Math::Pow(2, (i + 0.5) / N);
            pow2_1[i] = (float)(log2_N * Math::Pow(2, (i + 0.5) / N));
            pow2_2[i] = (float)(log2_N * log2_N * Math::Pow(2, (i + 0.5) / N) / 2);
            ipw2_0[i] = (float)Math::Pow(2, -(i + 0.5) / N);
            ipw2_1[i] = (float)(-log2_N * Math::Pow(2, -(i + 0.5) / N));
            ipw2_2[i] = (float)(log2_N * log2_N * Math::Pow(2, -(i + 0.5) / N) / 2);
        }

        saw0 = (float**)calloc(WT_N, sizeof(float*));
        saw1 = (float**)calloc(WT_N, sizeof(float*));
        saw2 = (float**)calloc(WT_N, sizeof(float*));
        tri0 = (float**)calloc(WT_N, sizeof(float*));
        tri1 = (float**)calloc(WT_N, sizeof(float*));
        tri2 = (float**)calloc(WT_N, sizeof(float*));
        imp0 = (float**)calloc(WT_N, sizeof(float*));
        imp1 = (float**)calloc(WT_N, sizeof(float*));
        imp2 = (float**)calloc(WT_N, sizeof(float*));

        for (int j = 0; j < WT_N; j++)
        {
            int N2 = WT_SIZE[j];

            saw0[j] = (float*)calloc(N2, sizeof(float));  // 配列の内容は0で初期化される
            saw1[j] = (float*)calloc(N2, sizeof(float));
            saw2[j] = (float*)calloc(N2, sizeof(float));
            tri0[j] = (float*)calloc(N2, sizeof(float));
            tri1[j] = (float*)calloc(N2, sizeof(float));
            tri2[j] = (float*)calloc(N2, sizeof(float));
            imp0[j] = (float*)calloc(N2, sizeof(float));
            imp1[j] = (float*)calloc(N2, sizeof(float));
            imp2[j] = (float*)calloc(N2, sizeof(float));

            double _2pi_N2 = 2 * Math::PI / N2;

            for (int i = 0; i < N2; i++)
            {
                for (int n = 1; n <= 1 << j; n++)
                {
                    saw0[j][i] += (float)(Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (n * Math::PI / 2));
                    saw1[j][i] += (float)(_2pi_N2 * n * Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (n * Math::PI / 2));
                    saw2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (2 * n * Math::PI / 2));

                    imp0[j][i] += (float)(Math::Cos(2 * Math::PI * n * (i + 0.5) / N2));
                    imp1[j][i] += (float)(-_2pi_N2 * n * Math::Sin(2 * Math::PI * n * (i + 0.5) / N2));
                    imp2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / 2);

                    if (n % 2 == 1)
                    {
                        tri0[j][i] += (float)(Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (n * n * Math::PI * Math::PI / 8));
                        tri1[j][i] += (float)(-_2pi_N2 * n * Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (n * n * Math::PI * Math::PI / 8));
                        tri2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (2 * n * n * Math::PI * Math::PI / 8));
                    }
                }
            }
        }

        // 私、ちゃんとメモリバリアしてるかな・・・？
        System::Threading::Volatile::Write(initialized, true);
    }

    double FastMath::Sin(double x)
    {
        if (!initialized) return 0;

        if (x < 0) x = -x;  // Fixme: xが負の場合
        double xr = x * N_2pi;
        int a = ((Int64)xr) & Mask;
        if (a < N / 2)
        {
            double d = xr - (Int64)xr - 0.5;

            return f0[a] + d * (f1[a] + d * f2[a]);
        }
        else
        {
            a = a & (N / 2 - 1);

            double d = xr - (Int64)xr - 0.5;

            return -(f0[a] + d * (f1[a] + d * f2[a]));
        }
    }

    double FastMath::Pow2(double x)
    {
        if (!initialized) return 0;

        //int integPart = (int)(x - ((int)x - 1)) + ((int)x - 1);  // floor(x) ← おまけ

        if (x >= 0)
        {
            double absx = x;
            int integPart = (int)absx;  // floor(x)

            if (integPart >= 63) return Math::Exp(Log2 * x);

            double xr = (absx - integPart) * N;
            int a = (int)xr;  // 0 ～ N-1

            double d = xr - a - 0.5;  // テイラー展開の基準点からの差

            //return (pow2_0[a] + d * (pow2_1[a] + d * pow2_2[a])) * ((Int64)1 << integPart);
            return (pow2_0[a] + d * pow2_1[a]) * ((Int64)1 << integPart);  // 指数関数はとても滑らか（ |f''(x)| / |f(x)| << 1 という意味で）
        }
        else
        {
            double absx = -x;
            int integPart = (int)absx;  // floor(x)

            if (integPart >= 63) return Math::Exp(Log2 * x);

            double xr = (absx - integPart) * N;
            int a = (int)xr;  // 0 ～ N-1

            double d = xr - a - 0.5;  // テイラー展開の基準点からの差

            //return (ipw2_0[a] + d * (ipw2_1[a] + d * ipw2_2[a])) * (0x4000000000000000L >> integPart) * INV_0x4000000000000000L;
            return (ipw2_0[a] + d * ipw2_1[a]) * ((0x4000000000000000L >> integPart) * INV_0x4000000000000000L);
            // うーん見た目が微妙だ・・・
        }
    }

    double inline FastMath::Saw(double x, int logovertone)  // 【お願い】xにあんまり大きな値を渡さないで・・・(2^50 くらいまではOK)
    {
        if (!initialized) return 0;

        if (logovertone >= WT_N) logovertone = WT_N - 1;  // あくまで保険
        if (logovertone < 0) logovertone = 0;  // あくまで保険

        double N2_2PI = WT_SIZE_2PI[logovertone];
        int mask = WT_MASK[logovertone];

        if (x < 0) x = -x;  // Fixme: xが負の場合
        double xr = x * N2_2PI;
        int a = (int)(((Int64)xr) & mask);
        double d = xr - (Int64)xr - 0.5;

        return saw0[logovertone][a] + d * (saw1[logovertone][a] + d * saw2[logovertone][a]);
    }

    double FastMath::Tri(double x, int logovertone)
    {
        if (!initialized) return 0;

        if (logovertone >= WT_N) logovertone = WT_N - 1;
        if (logovertone < 0) logovertone = 0;

        double N2_2PI = WT_SIZE_2PI[logovertone];
        int mask = WT_MASK[logovertone];

        if (x < 0) x = -x;
        double xr = x * N2_2PI;
        int a = (int)(((Int64)xr) & mask);
        double d = xr - (Int64)xr - 0.5;

        return tri0[logovertone][a] + d * (tri1[logovertone][a] + d * tri2[logovertone][a]);
    }

    double FastMath::Impulse(double x, int logovertone)
    {
        if (!initialized) return 0;

        if (logovertone >= WT_N) logovertone = WT_N - 1;
        if (logovertone < 0) logovertone = 0;

        double N2_2PI = WT_SIZE_2PI[logovertone];
        int mask = WT_MASK[logovertone];

        if (x < 0) x = -x;
        double xr = x * N2_2PI;
        int a = (int)(((Int64)xr) & mask);
        double d = xr - (Int64)xr - 0.5;

        return imp0[logovertone][a] + d * (imp1[logovertone][a] + d * imp2[logovertone][a]);
    }
}