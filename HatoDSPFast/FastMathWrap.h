#pragma once
namespace HatoDSPFast
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace System::Text;
    using namespace System::Threading::Tasks;

    public ref class FastMathWrap abstract sealed
    {
    public:
        static FastMathWrap();
        static double Sin(double x);
        static double Pow2(double x);
        static double Saw(double x, int logovertone);
        static double Tri(double x, int logovertone);
        static double Impulse(double x, int logovertone);

        static property bool Initialized{
    public:
            bool get();
        }
    };
}