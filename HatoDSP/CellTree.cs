using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class CellTree
    {
        Func<Cell> generator;

        public CellTree()
        {
        }

        public CellTree(Func<Cell> generator)
        {
            this.generator = generator;
        }

        public Cell Generate()
        {
            if (generator == null)
            {
                return null;
            }

            return generator();
        }
    }
}
