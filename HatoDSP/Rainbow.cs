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
        float detuneAmount = 0.2f;  // 0～
        float unisoneAmount = 1.0f;  // 0～1
        float stereoAmount = 1.0f;  // 0～1
        float[] rand;

        public Rainbow()
        {
            this.children = null;

            children = new CellTree(() => new NullCell());
            UpdateRainbowN();
        }

        public override void AssignChildren(CellWire[] children)
        {
            if (children.Length >= 1)
            {
                this.children = children[0].Source;  // FIXME: 複数指定

                UpdateRainbowN();  // ここで初期化するべきではない
            }
        }

        private void UpdateRainbowN()
        {
            List<Cell> list2 = new List<Cell>();  // 既存の割り当ては破棄

            for (int i = 0; i < rainbowN; i++)
            {
                list2.Add(children.Generate());
            }

            list = list2;

            Random r = new Random(57923741);
            float[] rand2 = new float[rainbowN];
            for (int i = 0; i < rainbowN; i++)
            {
                rand2[i] = (float)r.NextDouble();
            }

            rand = rand2;
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            // メモ：AssignControllersを呼んでいる間に、Takeを呼ばないでね！！

            if (ctrl.Length >= 1)
            {
                int newRainbowN = Math.Max(1, Math.Min(32, (int)(ctrl[0].Value + 0.5f)));
                if (rainbowN != newRainbowN)
                {
                    rainbowN = newRainbowN;
                    UpdateRainbowN();
                }
            }
            if (ctrl.Length >= 2)
            {
                detuneAmount = ctrl[1].Value;
            }
            if (ctrl.Length >= 3)
            {
                unisoneAmount = ctrl[2].Value;
            }
            if (ctrl.Length >= 4)
            {
                stereoAmount = ctrl[3].Value;
            }
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[]
                {
                    new CellParameterInfo("Count", true, 0.0f, 32.0f, 7.0f, x => (int)(x + 0.5) + ""),
                    new CellParameterInfo("Detune", true, 0.0f, 1.0f, 0.2f, x => (int)(x * 10000) * 0.01 + "%"),
                    new CellParameterInfo("Unison", true, 0.0f, 1.0f, 0.0f, x => (int)(x * 10000) * 0.01 + "cent"),
                    new CellParameterInfo("Stereo", true, 0.0f, 2.0f, 1.0f, x => (int)(x * 10000) * 0.01 + "%")
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
            float entireamp = (float)(1.0 / Math.Sqrt(rainbowN));
            // 正規分布の和の分散は分散の和になる
            // 従って振幅の期待値はその平方根に比例（雑な推論）
            // ただし、デチューン量が大きい場合はこの限りではない

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

                float width = (rainbowN - 1.0f) / 2.0f;  // 片側幅
                float width_inv = (rainbowN <= 1) ? 1.0f : 1 / width;

                LocalEnvironment lenv2 = lenv.Clone();
                lenv2.Buffer = buf2;
                lenv2.Pitch = Signal.Add(lenv.Pitch, new ConstantSignal(detuneAmount * (j - width + (rand[j] - 0.5f) * 0.9228f) * width_inv, count));

                if (unisoneAmount != 0)
                {
                    lenv2.Locals["phase"] = new ConstantSignal(unisoneAmount * 2.0f * (float)Math.PI * (j + (rand[j] - 0.5f) * 0.5392f) / (float)rainbowN, count);
                }

                x.Take(count, lenv2);

                var panL = (1 - stereoAmount * ((j - width) * width_inv)) * entireamp;
                var panR = (1 + stereoAmount * ((j - width) * width_inv)) * entireamp;

                if (chCount == 1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        lenv.Buffer[0][i] += buf2[0][i] * panL;
                        lenv.Buffer[1][i] += buf2[0][i] * panR;
                    }
                }
                else if (chCount == 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        lenv.Buffer[0][i] += buf2[0][i] * panL;
                        lenv.Buffer[1][i] += buf2[1][i] * panR;
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
