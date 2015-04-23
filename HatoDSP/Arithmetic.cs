using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class Arithmetic : Cell
    {
        public enum OperationType
        {
            AddSub = 0,  // 加減算 x1 + x2 + x3 + ... - y1 - y2 - y3 - ...
            MulDiv,  // 乗除算 x1 * x2 * x3 * ... / y1 / y2 / y3 / ...
            Sidechain,  // (x1 + x2 + x3 ...) * y1 * y2 * y3 ...
            Count
        }

        Cell[] child = new Cell[] { };
        int[] port;
        OperationType op = OperationType.AddSub;

        public override CellParameter[] ParamsList
        {
            get
            {
                return new CellParameter[] {
                    new CellParameter("Operation", false, 0, (float)OperationType.Count, (float)OperationType.AddSub, x => ((OperationType)(x + 0.5f)).ToString())
                };
            }
        }

        public override void AssignChildren(CellWire[] children)
        {
            this.child = children.Select(x => x.Source.Generate()).ToArray();
            this.port = children.Select(x => x.Port).ToArray();
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1)
            {
                op = (OperationType)(ctrl[0].Value + 0.5f);
            }
        }

        int outChCnt = 0;

        public override int ChannelCount
        {
            get
            {
                if (outChCnt == 0)
                {
                    if (child.Length == 0)
                    {
                        outChCnt = 1;
                    }
                    else
                    {
                        outChCnt = child.Select(x => x.ChannelCount).Max();
                    }
                }
                return outChCnt;
            }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            outChCnt = ChannelCount;  // ←二重代入

            LocalEnvironment lenv2 = lenv.Clone();

            switch (op)
            {
                case OperationType.MulDiv:
                    break;
                case OperationType.Sidechain:
                    break;
                case OperationType.AddSub:
                default:
                    foreach (var cel in child)
                    {
                        if (cel.ChannelCount == 1)
                        {
                            float[][] tempbuf = new float[1][] { new float[count] };

                            lenv2.Buffer = tempbuf;

                            cel.Take(count, lenv2);

                            for (int ch = 0; ch < outChCnt; ch++)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    lenv.Buffer[ch][i] += tempbuf[0][i];  // すべてのチャンネルに加算する
                                }
                            }
                        }
                        else
                        {
                            // TODO: チャンネル数が異なるときエラー
                            cel.Take(count, lenv);  // 元のバッファにそのまま加算する
                        }
                    }
                    break;
            }
        }
    }
}
