#pragma once
namespace HatoDSPFast{

    static public ref class FastMath
    {
    private:
        static float* f0;
        static float* f1;
        static float* f2;
        static float* pow2_0;
        static float* pow2_1;
        static float* pow2_2;
        static float** saw0;
        static float** saw1;
        static float** saw2;
        static float** tri0;
        static float** tri1;
        static float** tri2;
        static float** imp0;
        static float** imp1;
        static float** imp2;

        static double N_2pi;

        // �������̂��߂Ɋ�����const�Łi�������ɂȂ�̂��H�j
        static int WT_N = 8;  // wavetable n, saw��wavetable�̕�����
        static int N_SAW = 16;
        static double INV_2PI;

        // N = 512 �ŏ\���ȃT�C�Y���Ǝv���܂��i�ϕ����Ȃ��Ȃ�j
        static int N = 128;
        static int Mask = N - 1;

    public:
        static FastMath();
        static double Sin(double x); 
        static double Pow2(double x);
        static double Saw(double x, int logovertone);
        static double Tri(double x, int logovertone);
        static double Impulse(double x, int logovertone);
    };
}

