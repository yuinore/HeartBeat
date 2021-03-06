#pragma once
namespace HatoDSPFast
{
    using namespace System;

    // Static classes causing warnings.
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/75cc5b2e-0dd1-4efc-8d5a-1f497679d8e4/static-classes-causing-warnings

    // Static classes in C++.NET 
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/4f1606b3-7831-46a7-abbd-7b4ceb81f09a/static-classes-in-cnet?forum=netfxbcl

    // Abstract class cannot be sealed in c#? - Stack Overflow
    // http://stackoverflow.com/questions/19404589/abstract-class-cannot-be-sealed-in-c

    public class FastMath abstract sealed  // public static class
    {
    public:
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

        static bool initialized;
        static bool initializeStarted;

    public:
        // メモ：literal ではなく static const にすると、 C# からは普通の static 変数に見えてしまう。
        // 　そのため、public にするならば literal を付けたほうがよい。
        // 　　　　constとliteralの違い http://blogs.konuma.org/blog/2007/07/constliteral_65e2/
        //
        // 　abstract sealed なクラスには literal フィールドは持てないって書いてありますがそんなことはないですね・・・。
        // 　　　　C++/CLIでの定数の定義 改 http://schima.hatenablog.com/entry/20090311/1236740102
        // 　　　　abstract sealed & literal http://bytes.com/topic/net/answers/603702-abstract-sealed-literal
        //
        // 　あっ昔のC++/CLIのバグでしたか・・・
        // 　　　　C++/CLI static class and literal http://bytes.com/topic/net/answers/712514-c-cli-static-class-literal
        // 　　　　C++/CLI literal keyword bug http://blog.tosh.me/2012/02/ccli-literal-keyword-bug.html
        static bool FastMath::IsInitialized();
        static int Get_WT_N();

    private:
        const static int WT_N = 10;  // wavetable n, sawのwavetableの分割数。メモリ量は O(2^n) に比例。この値を変更する場合は、WT_SIZEの値も必ず修正すること。
        //literal int N_SAW = 64;  // wavetableの基準サイズ(個)。2のべき乗でなければならない
        static int* WT_SIZE;  // wavetableのサイズ(個)。添字にlogovertoneを取り、この配列の長さはWT_Nである。すべて2のべき乗でなければならない。
        static double* WT_SIZE_2PI;  // == WT_SIZE / (2.0 * PI)
        static int* WT_MASK;  // == WT_SIZE.Select(x => x - 1)
        // ↑具体的な定義は都合によりコンストラクタで。

        // N = 512 で十分なサイズだと思います（積分しないなら）
        static const int N = 512;
        static const int Mask = N - 1;
        static const double INV_0x4000000000000000L;

        static const bool chacheWavetable = true;

        // 各種一時変数
    private:
        static const double LOG2;
        static const double INV_2PI;
    private:
        static const double _2pi_N;
        static const double N_2pi;
        static const double log2_N;

        static void Initialize();

    public:
        static double Sin(double x);
        static double Cos(double x);
        static double Pow2(double x);
        static double Saw(double x, int logovertone);
        static double Tri(double x, int logovertone);
        static double Impulse(double x, int logovertone);
        // ↑Impulseは音量に注意する必要あり。全ての倍音が 1 の強さを持つ。(sawの基音の pi/2 倍。) 
        // 必要に応じてampを下げること。(AnalogOscillatorを参考に)
    };
}

