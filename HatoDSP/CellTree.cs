using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class CellTree  // HatoSynthでのモジュールの単位
    {
        Func<Cell> generator;
        public string Name;  // must be unique
        public int Port;  // connection type, 0, 1, ...
        CellTree[] children;
        Controller[] ctrl;
        
        public CellTree(Func<Cell> generator)
        {
            this.generator = generator;
        }

        public void AssignChildren(CellTree[] children)
        {
            this.children = children;  // カプセル化？？
        }

        public void AssignControllers(Controller[] ctrl)
        {
            this.ctrl = ctrl;  // カプセル化？？
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
            if (ctrl != null)
            {
                cell.AssignControllers(ctrl);
            }
            return cell;
        }
    }
}
