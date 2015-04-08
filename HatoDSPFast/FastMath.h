#pragma once
namespace HatoDSPFast{
    using namespace System;

    // Static classes causing warnings.
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/75cc5b2e-0dd1-4efc-8d5a-1f497679d8e4/static-classes-causing-warnings

    // Static classes in C++.NET 
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/4f1606b3-7831-46a7-abbd-7b4ceb81f09a/static-classes-in-cnet?forum=netfxbcl

    // Abstract class cannot be sealed in c#? - Stack Overflow
    // http://stackoverflow.com/questions/19404589/abstract-class-cannot-be-sealed-in-c

    public ref class FastMath abstract sealed  // public static class
    {
    private:
        static float* f0;
        static float* f1;
        static float* f2;
        static float* pow2_0;
        static float* pow2_1;
        static float* pow2_2;
        static float* ipw2_0;
        static float* ipw2_1;
        static float* ipw2_2;
        static float** saw0;
        static float** saw1;
        static float** saw2;
        static float** tri0;
        static float** tri1;
        static float** tri2;
        static float** imp0;
        static float** imp1;
        static float** imp2;

        static bool initialized = false;

    public:
        static const int WT_N = 10;  // wavetable n, saw��wavetable�̕������B�������ʂ� O(2^n) �ɔ��B���̒l��ύX����ꍇ�́AWT_SIZE�̒l���K���C�����邱�ƁB

    private:
        //static const int N_SAW = 64;  // wavetable�̊�T�C�Y(��)�B2�ׂ̂���łȂ���΂Ȃ�Ȃ�
        static int* WT_SIZE;  // wavetable�̃T�C�Y(��)�B�Y����logovertone�����A���̔z��̒�����WT_N�ł���B���ׂ�2�ׂ̂���łȂ���΂Ȃ�Ȃ��B
        static double* WT_SIZE_2PI;  // == WT_SIZE / (2.0 * PI)
        static int* WT_MASK;  // == WT_SIZE.Select(x => x - 1)
        // ����̓I�Ȓ�`�͓s���ɂ��R���X�g���N�^�ŁB

        // N = 512 �ŏ\���ȃT�C�Y���Ǝv���܂��i�ϕ����Ȃ��Ȃ�j
        static const int N = 512;
        static const int Mask = N - 1;
        static const double INV_0x4000000000000000L = 1.0 / (double)0x4000000000000000L;

        // �e��ꎞ�ϐ�
        static const double _2pi_N = 2.0 * Math::PI / N;
        static const double N_2pi = N / (2.0 * Math::PI);
        static const double log2_N = Math::Log(2.0) / N;

        static FastMath();
        static void Initialize();

    public:
        static const double INV_2PI = 1.0 / (2 * Math::PI);
        static const double Log2 = Math::Log(2.0);

        static double Sin(double x); 
        static double Pow2(double x);
        static double Saw(double x, int logovertone);
        static double Tri(double x, int logovertone);
        static double Impulse(double x, int logovertone);
        // ��Impulse�͉��ʂɒ��ӂ���K�v����B�S�Ă̔{���� 1 �̋��������B(saw�̊�� pi/2 �{�B) 
        // �K�v�ɉ�����amp�������邱�ƁB(AnalogOscillator���Q�l��)
    };
}

