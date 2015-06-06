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
    /// この機能により、多くのセルの実装の負担を和らげます。
    /// </summary>
    public abstract class SingleInputCell : Cell
    {
        /// <summary>
        /// 入力されたセルを表します。
        /// 添字の順に、ポート番号が、0, 1, 2,... となっています。
        /// 各ポートのセルはそれぞれ1つになるように加算合成されます。
        /// また、該当するセルが存在しない場合は、nullではなくNullCellが割り当てられます。
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

            int portCount = maxport + 1;  // 子セルが存在しない場合は、この値は0ではなく1となる。

            var lst = (new int[portCount]).Select(x => new List<CellTree>()).ToArray();

            for (int i = 0; i < children.Length; i++)
            {
                lst[children[i].Port].Add(children[i].Source);
            }

            InputCells = new Cell[portCount];

            for (int i = 0; i < portCount; i++)
            {
                if (lst[i].Count == 0)
                {
                    // 子セルは存在しない
                    InputCells[i] = new NullCell();
                }
                else if (lst[i].Count == 1)
                {
                    InputCells[i] = lst[i][0].Generate();
                }
                else
                {
                    Arithmetic cel = new Arithmetic();
                    cel.AssignChildren(lst[i].Select(x => new CellWire(x, 0)).ToArray());
                    cel.AssignControllers(new CellParameterValue[] { new CellParameterValue((float)Arithmetic.OperationType.AddSub) });

                    InputCells[i] = cel;
                }
            }
        }

        //public sealed override void Take(int count, LocalEnvironment lenv)

        //public abstract int ChannelCount { get; }

        //public abstract void AssignControllers(CellParameterValue[] ctrl);

        //public abstract CellParameter[] ParamsList { get; }
    }
}
