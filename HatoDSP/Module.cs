using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public abstract class Module  // 一般のシステムを表す（TODO:このクラス消す）
    {
        public abstract float[][] Take(int count, float[][][] input);
    }
}
