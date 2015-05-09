#include "stdafx.h"
#include "FastMath.h"
#include "WaveFile.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <xmmintrin.h>
#include <immintrin.h>

void aaa() {
    System::Windows::Forms::MessageBox::Show("trace : " + System::DateTime::Now.Second + ", " + System::DateTime::Now.Millisecond);
}

namespace HatoDSPFast {
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace System::Text;
    using namespace System::Threading::Tasks;

    // https://msdn.microsoft.com/ja-jp/library/acxkb76w.aspx
    const double FastMath::INV_0x4000000000000000L = 1.0 / 0x4000000000000000L;

    const double FastMath::LOG2 = 0.69314718055994530941723212145818;  // �����ꑽ��4�{���x
    const double FastMath::INV_2PI = 1.0 / (2 * Math::PI);

    const double FastMath::_2pi_N = 2.0 * Math::PI / N;
    const double FastMath::N_2pi = N / (2.0 * Math::PI);
    const double FastMath::log2_N = LOG2 / N;

    bool FastMath::initialized = false;
    bool FastMath::initializeStarted = false;

    //****** �������̂Ȃ��ϐ�����
    float* FastMath::f0;
    float* FastMath::f1;
    float* FastMath::f2;
    float* FastMath::pow2_0;
    float* FastMath::pow2_1;
    float* FastMath::pow2_2;
    float* FastMath::ipw2_0;
    float* FastMath::ipw2_1;
    float* FastMath::ipw2_2;
    float** FastMath::saw0;
    float** FastMath::saw1;
    float** FastMath::saw2;
    float** FastMath::tri0;
    float** FastMath::tri1;
    float** FastMath::tri2;
    float** FastMath::imp0;
    float** FastMath::imp1;
    float** FastMath::imp2;

    int* FastMath::WT_SIZE;  // wavetable�̃T�C�Y(��)�B�Y����logovertone�����A���̔z��̒�����WT_N�ł���B���ׂ�2�ׂ̂���łȂ���΂Ȃ�Ȃ��B
    double* FastMath::WT_SIZE_2PI;  // == WT_SIZE / (2.0 * PI)
    int* FastMath::WT_MASK;  // == WT_SIZE.Select(x => x - 1)
    //******

    int FastMath::Get_WT_N() {
        return WT_N;
    }

    bool FastMath::IsInitialized()
    {
        // TODO: �������o���A
        if (!initializeStarted) {
            initializeStarted = true;
            Task::Factory->StartNew(gcnew Action(Initialize));  // �}�l�[�W�R�[�h���������Ă邯�ǂ����̂��H�H
        }

        return initialized;

        // �f���Q�[�g�ƃ��\�b�h�O���[�v�i���\�b�h��`�j�̈Ⴂ�Ƃ�

        // Delegate�ׂ̍������� - ��z�� in C# - �͂Ăȃ_�C�A���[
        // http://d.hatena.ne.jp/toshi_m/20100717/1279368502
    }

    void FastMath::Initialize() {
        WT_SIZE = (int*)calloc(WT_N, sizeof(int));
        WT_SIZE_2PI = (double*)calloc(WT_N, sizeof(double));
        WT_MASK = (int*)calloc(WT_N, sizeof(int));
        char fname[100];

        // �����̒l��ύX����ƁA�L���b�V���������ɂȂ�̂Œ��ӁB�蓮�ō폜����K�v������܂��B

        /* //**********************************************
        //int temp[WT_N] = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };  // ���Z���x�̃e�X�g�p
        int temp[WT_N] = { 65536, 65536, 65536, 65536, 65536, 65536, 65536, 65536, 65536, 65536 };
        /*/ //**********************************************
        int temp[WT_N] = { 8, 512, 512, 512, 1024, 1024, 2048, 2048, 4096, 4096 };  // �����Ă킩��₷�����邽�߂� temp[0]=8 ��
        // N_SAW���Z��   { 8, 256, 128, 64,  64,   32,   32,   16,   16,   8 };  // �ō����g���̔{��1����������Ɋ������T���v����
        //*/ //*********************************************

        for (int j = 0; j < WT_N; j++) {
            WT_SIZE[j] = temp[j];
            WT_SIZE_2PI[j] = temp[j] / (2.0 * Math::PI);
            WT_MASK[j] = temp[j] - 1;
        }

        //************* �z��̃������m�� *************
        f0 = (float*)calloc(N / 2, sizeof(float));
        f1 = (float*)calloc(N / 2, sizeof(float));
        f2 = (float*)calloc(N / 2, sizeof(float));
        pow2_0 = (float*)calloc(N, sizeof(float));
        pow2_1 = (float*)calloc(N, sizeof(float));
        pow2_2 = (float*)calloc(N, sizeof(float));
        ipw2_0 = (float*)calloc(N, sizeof(float));
        ipw2_1 = (float*)calloc(N, sizeof(float));
        ipw2_2 = (float*)calloc(N, sizeof(float));

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

            saw0[j] = (float*)calloc(N2, sizeof(float));  // �z��̓��e��0�ŏ����������
            saw1[j] = (float*)calloc(N2, sizeof(float));
            saw2[j] = (float*)calloc(N2, sizeof(float));
            tri0[j] = (float*)calloc(N2, sizeof(float));
            tri1[j] = (float*)calloc(N2, sizeof(float));
            tri2[j] = (float*)calloc(N2, sizeof(float));
            imp0[j] = (float*)calloc(N2, sizeof(float));
            imp1[j] = (float*)calloc(N2, sizeof(float));
            imp2[j] = (float*)calloc(N2, sizeof(float));
        }

        //************* �����̃L���b�V���̃`�F�b�N�i�ǂݍ��߂��ꍇ��return�j *************
        if (System::IO::File::Exists(HatoLib::HatoPath::FromAppDir("cache\\wavetable\\initialized"))) {
            //Task::Run(gcnew Action(aaa));
            // �{�g���l�b�N�̓t�@�C���ǂݍ��݂ł͂Ȃ��炵���B

            try {
                WaveFile::ReadAllSamples("cache\\wavetable\\sin0.wav", &f0, 1, N / 2);
                WaveFile::ReadAllSamples("cache\\wavetable\\sin1.wav", &f1, 1, N / 2);
                WaveFile::ReadAllSamples("cache\\wavetable\\sin2.wav", &f2, 1, N / 2);
                WaveFile::ReadAllSamples("cache\\wavetable\\pow2_0.wav", &pow2_0, 1, N);
                WaveFile::ReadAllSamples("cache\\wavetable\\pow2_1.wav", &pow2_1, 1, N);
                WaveFile::ReadAllSamples("cache\\wavetable\\pow2_2.wav", &pow2_2, 1, N);
                WaveFile::ReadAllSamples("cache\\wavetable\\ipw2_0.wav", &ipw2_0, 1, N);
                WaveFile::ReadAllSamples("cache\\wavetable\\ipw2_1.wav", &ipw2_1, 1, N);
                WaveFile::ReadAllSamples("cache\\wavetable\\ipw2_2.wav", &ipw2_2, 1, N);

                for (int j = 0; j < WT_N; j++)
                {
                    int N2 = WT_SIZE[j];

                    sprintf(fname, "cache\\wavetable\\saw0[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &saw0[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\saw1[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &saw1[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\saw2[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &saw2[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\tri0[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &tri0[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\tri1[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &tri1[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\tri2[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &tri2[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\imp0[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &imp0[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\imp1[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &imp1[j], 1, WT_SIZE[j]);
                    sprintf(fname, "cache\\wavetable\\imp2[%d].wav", j);
                    WaveFile::ReadAllSamples(fname, &imp2[j], 1, WT_SIZE[j]);
                }

                // ����ɏI��
                //Task::Run(gcnew Action(aaa));

                System::Threading::Volatile::Write(initialized, true);

                return;
            }
            catch (System::IO::FileNotFoundException^) {
            }
        }
        //************* �z��̃f�[�^�̐��� *************
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

        for (int j = 0; j < WT_N; j++)
        {
            int N2 = WT_SIZE[j];

            double _2pi_N2 = 2 * Math::PI / N2;

            for (int i = 0; i < N2; i++)
            {
                for (int n = 1 << j; n >= 1; n--)  // �덷�����炷���߂ɏ�����(�Ɗ��҂����)��������Z����
                {
                    // n := �{���̃C���f�b�N�X(1-origin)

                    /* //**** ���֐���v�@ ****
                    saw0[j][i] += (float)(Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (n * Math::PI / 2));
                    saw1[j][i] += (float)(_2pi_N2 * n * Math::Cos(2 * Math::PI * n * (i + 0.5) / N2) / (n * Math::PI / 2));
                    saw2[j][i] += (float)(-_2pi_N2 * _2pi_N2 * n * n * Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (2 * n * Math::PI / 2));
                    /*/ //**** �S��A���@ ****
                    double  y1 = Math::Sin(2 * Math::PI * n * (i + 0.0) / N2) / (n * Math::PI / 2);
                    double  y2 = Math::Sin(2 * Math::PI * n * (i + 0.5) / N2) / (n * Math::PI / 2);
                    double  y3 = Math::Sin(2 * Math::PI * n * (i + 1.0) / N2) / (n * Math::PI / 2);
                    saw0[j][i] += (float)y2;
                    saw1[j][i] += (float)(y3 - y1);
                    saw2[j][i] += (float)(2 * y1 - 4 * y2 + 2 * y3);
                    //*/

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

        //************* �����������̒ʒm *************

        // ���A�����ƃ������o���A���Ă邩�ȁE�E�E�H(�������ĂȂ�)
        System::Threading::Volatile::Write(initialized, true);

        //************* �t�@�C���ւ̃L���b�V���̏������� *************
        if (chacheWavetable) {
            if (!System::IO::Directory::Exists(HatoLib::HatoPath::FromAppDir("cache\\"))) {  // gcnew String �͏ȗ��ł���H�H
                System::IO::Directory::CreateDirectory(HatoLib::HatoPath::FromAppDir("cache\\"));
            }
            if (!System::IO::Directory::Exists(HatoLib::HatoPath::FromAppDir("cache\\wavetable\\"))) {
                System::IO::Directory::CreateDirectory(HatoLib::HatoPath::FromAppDir("cache\\wavetable\\"));
            }

            WaveFile::WriteAllSamples("cache\\wavetable\\sin0.wav", &f0, 1, N / 2, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\sin1.wav", &f1, 1, N / 2, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\sin2.wav", &f2, 1, N / 2, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\pow2_0.wav", &pow2_0, 1, N, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\pow2_1.wav", &pow2_1, 1, N, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\pow2_2.wav", &pow2_2, 1, N, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\ipw2_0.wav", &ipw2_0, 1, N, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\ipw2_1.wav", &ipw2_1, 1, N, 44100, 32);
            WaveFile::WriteAllSamples("cache\\wavetable\\ipw2_2.wav", &ipw2_2, 1, N, 44100, 32);

            for (int j = 0; j < WT_N; j++)
            {
                int N2 = WT_SIZE[j];

                sprintf(fname, "cache\\wavetable\\saw0[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &saw0[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\saw1[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &saw1[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\saw2[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &saw2[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\tri0[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &tri0[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\tri1[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &tri1[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\tri2[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &tri2[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\imp0[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &imp0[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\imp1[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &imp1[j], 1, WT_SIZE[j], 44100, 32);
                sprintf(fname, "cache\\wavetable\\imp2[%d].wav", j);
                WaveFile::WriteAllSamples(fname, &imp2[j], 1, WT_SIZE[j], 44100, 32);
            }

            WaveFile::WriteAllSamples("cache\\wavetable\\initialized", &f0, 1, 1, 44100, 32);
        }
    }

    double FastMath::Cos(double x)
    {
        return Sin(x + 0.5 / Math::PI);
    }

	double FastMath::Sin(double x)
    {
        if (!initialized) {
            IsInitialized();  // �������w��
            return 0;
        }

        double xr = x * N_2pi;
        Int32 ixr = (Int32)xr;
        if (xr < 0) ixr -= 1;  // x�����̏ꍇ�̊ۂߕ␳
        int a = (int)(ixr & Mask);
        double d = xr - ixr - 0.5;  // xr�����傤�ǐ����̂Ƃ��́A�ۂ߂̓s����Ad = x >= 0 ? -0.5 : +0.5 �ƂȂ�

        if (a < N / 2)
        {
            return f0[a] + d * (f1[a] + d * f2[a]);
        }
        else
        {
            a = a & (N / 2 - 1);

            return -(f0[a] + d * (f1[a] + d * f2[a]));
        }
    }

    double FastMath::Pow2(double x)
    {
        if (!initialized) {
            IsInitialized();  // �������w��
            return 0;
        }

        //int integPart = (int)(x - ((int)x - 1)) + ((int)x - 1);  // floor(x) �� ���܂�

        if (x >= 0)
        {
            double absx = x;
            int integPart = (int)absx;  // floor(x)

            if (integPart >= 63) return Math::Exp(LOG2 * x);

            double xr = (absx - integPart) * N;
            int a = (int)xr;  // 0 �` N-1

            double d = xr - a - 0.5;  // �e�C���[�W�J�̊�_����̍�

            //return (pow2_0[a] + d * (pow2_1[a] + d * pow2_2[a])) * ((Int32)1 << integPart);
            return (pow2_0[a] + d * pow2_1[a]) * (((__int64)1) << integPart);  // �w���֐��͂ƂĂ����炩�i |f''(x)| / |f(x)| << 1 �Ƃ����Ӗ��Łj
            // ___________________________________ �����Int32�ɂ���Ȃ�I
        }
        else
        {
            double absx = -x;
            int integPart = (int)absx;  // floor(x)

            if (integPart >= 63) return Math::Exp(LOG2 * x);

            double xr = (absx - integPart) * N;
            int a = (int)xr;  // 0 �` N-1

            double d = xr - a - 0.5;  // �e�C���[�W�J�̊�_����̍�

            //return (ipw2_0[a] + d * (ipw2_1[a] + d * ipw2_2[a])) * (0x4000000000000000L >> integPart) * INV_0x4000000000000000L;
            return (ipw2_0[a] + d * ipw2_1[a]) * ((0x4000000000000000L >> integPart) * INV_0x4000000000000000L);
            // ���[�񌩂��ڂ��������E�E�E
        }
    }

    double /*inline*/ FastMath::Saw(double x, int logovertone)  // �y���肢�zx�ɂ���܂�傫�Ȓl��n���Ȃ��ŁE�E�E( 1000rad ���炢�܂łł��肢)
    {
        if (!initialized) {
            IsInitialized();  // �������w��
            return 0;
        }

        if (logovertone >= WT_N) logovertone = WT_N - 1;  // �����܂ŕی�
        if (logovertone < 0) logovertone = 0;  // �����܂ŕی�

        double N2_2PI = WT_SIZE_2PI[logovertone];
        int mask = WT_MASK[logovertone];

        double xr = x * N2_2PI;
        Int32 ixr = (Int32)xr;
        if (xr < 0) ixr -= 1;  // x�����̏ꍇ�̊ۂߕ␳
        int a = (int)(ixr & mask);
        double d = xr - ixr - 0.5;  // xr�����傤�ǐ����̂Ƃ��́A�ۂ߂̓s����Ad = x >= 0 ? -0.5 : +0.5 �ƂȂ�

        //float* aa = new __declspec(align(32)) float[8];  // �����F�A���C�������g
        // gcc : __attribute__
        //__m256 w = _mm256_load_ps(aa);

        return saw0[logovertone][a] + d * (saw1[logovertone][a] + d * saw2[logovertone][a]);
    }

    double FastMath::Tri(double x, int logovertone)
    {
        if (!initialized) {
            IsInitialized();  // �������w��
            return 0;
        }

        if (logovertone >= WT_N) logovertone = WT_N - 1;
        if (logovertone < 0) logovertone = 0;

        double N2_2PI = WT_SIZE_2PI[logovertone];
        int mask = WT_MASK[logovertone];

        if (x < 0) x = -x;
        double xr = x * N2_2PI;
        int a = (int)(((Int32)xr) & mask);
        double d = xr - (Int32)xr - 0.5;

        return tri0[logovertone][a] + d * (tri1[logovertone][a] + d * tri2[logovertone][a]);
    }

    double FastMath::Impulse(double x, int logovertone)
    {
        if (!initialized) {
            IsInitialized();  // �������w��
            return 0;
        }

        if (logovertone >= WT_N) logovertone = WT_N - 1;
        if (logovertone < 0) logovertone = 0;

        double N2_2PI = WT_SIZE_2PI[logovertone];
        int mask = WT_MASK[logovertone];

        if (x < 0) x = -x;
        double xr = x * N2_2PI;
        int a = (int)(((Int32)xr) & mask);
        double d = xr - (Int32)xr - 0.5;

        return imp0[logovertone][a] + d * (imp1[logovertone][a] + d * imp2[logovertone][a]);
    }
}