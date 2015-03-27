using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public abstract class Cell  // HatoSynthでのモジュールの単位
    {
        public abstract void AssignChildren(CellTree[] children);  // paramsは厄介なのでやめましょう

        public abstract Signal[] Take(int count, LocalEnvironment lenv);
    }
}
