using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class Rainbow : Cell
    {
        CellTree children;
        List<Cell> list;
        int rainbowN = 3;

        public override void AssignChildren(CellTree children)
        {
            this.children = children;

            list = new List<Cell>();
            for (int i = 0; i < rainbowN; i++)
            {
                list.Add(children.Generate());
            }
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            Signal[] sum = null;

            foreach (var x in list)
            {
                // lenvのピッチをここで加工する

                var sig = x.Take(count, lenv);
                if (sum == null)
                {
                    sum = (new Signal[sig.Length]).Select(nil => new ConstantSignal(0, count)).ToArray();
                }
                for (int i = 0; i < sig.Length; i++)
                {
                    sum[i] = Signal.Add(sum[i], sig[i]);
                }
            }

            return sum;
        }
    }
}
