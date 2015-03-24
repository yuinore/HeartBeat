using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class AnalogOscillator : Cell
    {
        CellTree children;
        SinGenerator gen;

        public AnalogOscillator()
        {
            gen = new SinGenerator();
        }
        public override void AssignChildren(CellTree children)
        {
            this.children = children;
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            var ret = gen.Take(count, new Signal[][] { });

            return ret;
        }
    }
}
