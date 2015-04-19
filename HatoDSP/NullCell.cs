using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    internal class NullCell : Cell
    {
        public override CellParameter[] ParamsList
        {
            get { return new CellParameter[] { }; }
        }

        public override void AssignChildren(CellWire[] children)
        {
            return;
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            return;
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            return new Signal[] { new ConstantSignal(0, count) };
        }
    }
}
