#include "stdafx.h"
#include "FastMathWrap.h"
#include "FastMath.h"

namespace HatoDSPFast
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace System::Text;
    using namespace System::Threading::Tasks;

    FastMathWrap::FastMathWrap()
    {
    }

    double FastMathWrap::Sin(double x){ return FastMath::Sin(x); }
    double FastMathWrap::Pow2(double x){ return FastMath::Pow2(x); }
    double FastMathWrap::Saw(double x, int logovertone){ return FastMath::Saw(x, logovertone); }
    double FastMathWrap::Tri(double x, int logovertone){ return FastMath::Tri(x, logovertone); }
    double FastMathWrap::Impulse(double x, int logovertone){ return FastMath::Impulse(x, logovertone); }

    bool FastMathWrap::Initialized::get(){
        return FastMath::IsInitialized();
    }
}