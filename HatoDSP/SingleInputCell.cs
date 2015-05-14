using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    /// <summary>
    /// Arithmetic以外の多くのセルがそうであるように、
    /// 同じポートへの入力を加算合成して、子クラスに渡します。
    /// </summary>
    public abstract class SingleInputCell : Cell
    {
        /// <summary>
        /// 入力されたセルを表します。
        /// 添字の順に、ポート番号が、0, 1, 2,... となっています。
        /// 各ポートのセルはそれぞれ1つまでに制限されます。
        /// </summary>
        protected Cell[] InputCells = new Cell[] { new NullCell() };

        private CellWire[] originalCells;

        public sealed override void AssignChildren(CellWire[] children)
        {
            originalCells = children;

            int maxport = 0;

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].Port > maxport) maxport = children[i].Port;
            }

            int portCount = maxport + 1;

            var lst = (new int[portCount]).Select(x => new List<CellTree>()).ToArray();

            for (int i = 0; i < children.Length; i++)
            {
                lst[children[i].Port].Add(children[i].Source);
            }

            InputCells = new Cell[portCount];

            for (int i = 0; i < portCount; i++)
            {
                Arithmetic cel = new Arithmetic();
                cel.AssignChildren(lst[i].Select(x => new CellWire(x, 0)).ToArray());  // 子は0個でもよい
                cel.AssignControllers(new CellParameterValue[] { new CellParameterValue((float)Arithmetic.OperationType.AddSub) });

                InputCells[i] = cel;
            }
        }

        //public sealed override void Take(int count, LocalEnvironment lenv)

        //public abstract int ChannelCount { get; }

        //public abstract void AssignControllers(CellParameterValue[] ctrl);

        //public abstract CellParameter[] ParamsList { get; }
    }
}
