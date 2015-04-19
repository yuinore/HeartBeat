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
        int rainbowN = 7;
        float[] rand;

        public Rainbow()
        {
            this.children = null;

            list = new List<Cell>();  // 初期化
            for (int i = 0; i < rainbowN; i++)
            {
                list.Add(new NullCell());
            }

            Random r = new Random(57923741);
            rand = new float[rainbowN];
            for (int i = 0; i < rainbowN; i++)
            {
                rand[i] = (float)r.NextDouble();
            }
        }

        public override void AssignChildren(CellWire[] children)
        {
            if (children.Length >= 1)
            {
                this.children = children[0].Source;  // FIXME: 複数指定

                list = new List<Cell>();  // 既存の割り当ては破棄

                for (int i = 0; i < rainbowN; i++)
                {
                    list.Add(children[0].Source.Generate());
                }
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            // TODO:
        }

        public override CellParameter[] ParamsList
        {
            get
            {
                return new CellParameter[]{
                };
            }
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            Signal[] sumL = new Signal[list.Count];
            Signal[] sumR = new Signal[list.Count];
            Signal originalPitch = lenv.Pitch;

            for (int j = 0; j < list.Count; j++)
            {
                var x = list[j];

                // lenvのピッチをここで加工する

                lenv.Pitch = Signal.Add(originalPitch, new ConstantSignal(0.2f * (j - (rainbowN - 1.0f) / 2 + (rand[j] - 0.5f) * 1.0f) / ((rainbowN - 1.0f) / 2), count));

                var sig = x.Take(count, lenv);

                float width = (rainbowN - 1.0f) / 2.0f;  // 片側幅
                var panL = new ConstantSignal(1 - 1.0f * ((j - width) / width), count);
                var panR = new ConstantSignal(1 + 1.0f * ((j - width) / width), count);

                /*
                for (int i = 0; i < sig.Length; i++)
                {
                    sum[i] = Signal.Add(
                        Signal.Multiply(sig[i], i % 2 == 0 ? panL : panR),
                        sum[i]);
                }*/

                if (sig.Length == 1)
                {
                    sumL[j] = Signal.Multiply(sig[0], panL);
                    sumR[j] = Signal.Multiply(sig[0], panR);
                }
                else if (sig.Length == 2)
                {
                    sumL[j] = Signal.Multiply(sig[0], panL);
                    sumR[j] = Signal.Multiply(sig[1], panR);
                }
                else
                {
                    try { throw new NotImplementedException("未実装"); }
                    catch { }
                }
            }

            return new Signal[] {
                Signal.AddRange(sumL),
                Signal.AddRange(sumR)
            };
        }
    }
}
