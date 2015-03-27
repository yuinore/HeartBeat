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
        string Name;  // must be unique
        int Port;  // connection type, 0, 1, ...
        CellTree[] children;

        public CellTree()
        {
        }

        public CellTree(Func<Cell> generator)
        {
            this.generator = generator;
        }

        public void AssignChildren(CellTree[] children)
        {
            this.children = children;
        }

        public Cell Generate()
        {
            if (generator == null)
            {
                return null;
            }

            Cell cell = generator();
            if (children != null)
            {
                cell.AssignChildren(children);
            }
            return cell;
        }
    }
}
