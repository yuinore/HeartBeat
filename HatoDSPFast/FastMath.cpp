#include "stdafx.h"
#include "FastMath.h"
#include <stdlib.h>

namespace HatoDSPFast {
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace System::Text;
    using namespace System::Threading::Tasks;

    // ������K�v�ɂȂ�܂ŌĂ΂�Ȃ�����Ƃ����ĐÓI�R���X�g���N�^�ŏd��������������̂͗ǂ��Ȃ��Ǝv���܂��B
    static FastMath::FastMath()
    {
        f0 = (float*)calloc(N / 2, sizeof(float));
        f1 = (float*)calloc(N / 2, sizeof(float));
        f2 = (float*)calloc(N / 2, sizeof(float));
        pow2_0 = (float*)calloc(N, sizeof(float));
        pow2_1 = (float*)calloc(N, sizeof(float));
        pow2_2 = (float*)calloc(N, sizeof(float));

        double _2pi_N = 2 * Math::PI / N;
        double log2_N = Math::Log(2.0) / N;
        INV_2PI = 1.0 / (2 * Math::PI);
        N_2pi = (1.0 / _2pi_N);

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
            int N2 = N_SAW << j;

            saw0[j] = (float*)calloc(N2, sizeof(float));  // �z��̓��e��0�ŏ����������
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

                    imp0[j][i] += (float)(Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (Math::PI / 2));
                    imp1[j][i] += (float)(-_2pi_N2 * n * Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (Math::PI / 2));
                    imp2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (2 * Math::PI / 2));

                    if (n % 2 == 1)
                    {
                        tri0[j][i] += (float)(Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (n * n * Math::PI * Math::PI / 8));
                        tri1[j][i] += (float)(-_2pi_N2 * n * Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (n * n * Math::PI * Math::PI / 8));
                        tri2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (2 * n * n * Math::PI * Math::PI / 8));
                    }
                }
            }
        }
    }

    double FastMath::Sin(double x)
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

    double FastMath::Pow2(double x)
    {
        //int integPart = (int)(x - ((int)x - 1)) + ((int)x - 1);  // floor(x) �� ���܂�

        if (x >= 0)
        {
            double absx = x;
            int integPart = (int)absx;  // floor(x)
            double xr = (absx - integPart) * N;
            int a = (int)xr;  // 0 �` N-1

            double d = xr - a - 0.5;  // �e�C���[�W�J�̊�_����̍�

            return (pow2_0[a] + d * (pow2_1[a] + d * pow2_2[a])) * (1 << integPart);
        }
        else
        {
            double absx = -x;
            int integPart = (int)absx;  // floor(x)
            double xr = (absx - integPart) * N;
            int a = (int)xr;  // 0 �` N-1

            double d = xr - a - 0.5;  // �e�C���[�W�J�̊�_����̍�

            return 1.0f / ((pow2_0[a] + d * (pow2_1[a] + d * pow2_2[a])) * (1 << integPart));  // �t����Ԃ��i������ƒx���j
        }
    }

    double FastMath::Saw(double x, int logovertone)
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

    double FastMath::Tri(double x, int logovertone)
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

    double FastMath::Impulse(double x, int logovertone)
    {
        if (logovertone >= WT_N) logovertone = WT_N - 1;
        if (logovertone < 0) logovertone = 0;

        int N2 = N_SAW << logovertone;
        int mask = N2 - 1;

        if (x < 0) x = -x;  // Fixme:
        double xr = x * N2 * INV_2PI;  // TODO:���Z�̍폜
        int a = ((int)xr) & mask;
        double d = xr - (int)xr - 0.5;

        return imp0[logovertone][a] + d * (imp1[logovertone][a] + d * imp2[logovertone][a]);
    }
}