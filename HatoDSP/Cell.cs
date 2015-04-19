using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public abstract class Cell
    {
        // ↓必ず呼ばれるとは限らない
        public abstract void AssignChildren(CellTree[] children);  // paramsは厄介なのでやめましょう

        // ↓必ず呼ばれるとは限らない
        public abstract void AssignControllers(CellParameterValue[] ctrl);

        public abstract Signal[] Take(int count, LocalEnvironment lenv);

        // パラメータがない場合でもnullを返してはならない。代わりに new CellParameter[]{} を返すこと。
        public abstract CellParameter[] ParamsList { get; }
    }
}
