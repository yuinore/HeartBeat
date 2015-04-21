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

        public override int ChannelCount
        {
            get { return 2; }
        }

        float[][] buf2;

        public override void Take(int count, LocalEnvironment lenv)
        {
            Signal[] sumL = new Signal[list.Count];
            Signal[] sumR = new Signal[list.Count];
            Signal originalPitch = lenv.Pitch;
            float[][] sum = lenv.Buffer;

            for (int j = 0; j < list.Count; j++)
            {
                var x = list[j];
                int chCount = x.ChannelCount;

                // lenvのピッチをここで加工する

                if (buf2 == null || buf2.Length < chCount || buf2[0].Length < count)
                {
                    buf2 = (new float[chCount][]).Select(y => new float[count]).ToArray();
                }

                for (int ch = 0; ch < chCount; ch++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        buf2[ch][i] = 0;
                    }
                }

                var lenv2 = new LocalEnvironment()
                {
                    Buffer = buf2,  // 別に用意した空のバッファを与える
                    Freq = lenv.Freq,
                    Gate = lenv.Gate,
                    Locals = lenv.Locals,
                    Pitch = Signal.Add(originalPitch, new ConstantSignal(0.2f * (j - (rainbowN - 1.0f) / 2 + (rand[j] - 0.5f) * 1.0f) / ((rainbowN - 1.0f) / 2), count)),
                    SamplingRate = lenv.SamplingRate
                };

                x.Take(count, lenv2);

                float width = (rainbowN - 1.0f) / 2.0f;  // 片側幅
                var panL = 1 - 1.0f * ((j - width) / width);
                var panR = 1 + 1.0f * ((j - width) / width);

                if (chCount == 1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        sum[0][i] += buf2[0][i] * panL;
                        sum[1][i] += buf2[0][i] * panR;
                    }
                }
                else if (chCount == 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        sum[0][i] += buf2[0][i] * panL;
                        sum[1][i] += buf2[1][i] * panR;
                    }
                }
                else
                {
                    try { throw new NotImplementedException("未実装"); }
                    catch { }
                }
            }
        }
    }
}
