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
        CellParameterValue[] ctrl;
        
        public CellTree(Func<Cell> generator)
        {
            this.generator = generator;  // CellTree<T> where T:Cell にして new T() しても良かったかもしれないですね・・・。
        }

        public CellTree(string name, string module)
        {
            Name = name;

            switch (module.ToLower())
            {
                case "analog filter":
                    generator = () => new BiquadFilter();
                    break;
                case "analog osc":
                    generator = () => new AnalogOscillator();
                    break;
                case "adsr":
                    generator = () => new ADSR();
                    break;
                case "rainbow":
                    generator = () => new Rainbow();
                    break;
                default:
                    throw new PatchFormatException("モジュール " + module + " は存在しません。");
            }
        }

        public void AssignChildren(CellTree[] children)
        {
            this.children = children;  // カプセル化は？？
        }

        public void AssignControllers(CellParameterValue[] ctrl)
        {
            this.ctrl = ctrl;  // カプセル化は？？
        }

        public void AssignControllers(float[] ctrl)  // 便宜的に
        {
            this.ctrl = ctrl.Select(x => new CellParameterValue(x)).ToArray();
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
