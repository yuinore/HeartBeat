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

            float[][] bufx = null, bufy = null;

            if (op == OperationType.MulDiv || op == OperationType.Sidechain)
            {
                bufx = new float[outChCnt][];  // TODO: optimization
                bufy = new float[outChCnt][];
                for (int ch = 0; ch < outChCnt; ch++)
                {
                    bufx[ch] = new float[count];
                    bufy[ch] = new float[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (op == OperationType.MulDiv)
                        {
                            bufx[ch][i] = 1;
                        }
                        bufy[ch][i] = 1;
                    }
                }
            }

            for (int celId = 0; celId < child.Length; celId++)
            {
                var cel = child[celId];

                float[][] tempbuf = new float[cel.ChannelCount][];

                for (int ch = 0; ch < cel.ChannelCount; ch++)
                {
                    tempbuf[ch] = new float[count];
                }

                lenv2.Buffer = tempbuf;

                cel.Take(count, lenv2);

                for (int ch = 0; ch < outChCnt; ch++)
                {
                    int srcch = cel.ChannelCount == 1 ? 0 : ch;  // 送り元チャンネル

                    if (port[celId] == 0)
                    {
                        switch (op)
                        {
                            case OperationType.MulDiv:
                                for (int i = 0; i < count; i++) { bufx[ch][i] *= tempbuf[srcch][i]; }
                                break;
                            case OperationType.Sidechain:
                                for (int i = 0; i < count; i++) { bufx[ch][i] += tempbuf[srcch][i]; }
                                break;
                            case OperationType.AddSub:
                            default:
                                for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += tempbuf[srcch][i]; }
                                break;
                        }
                    }
                    else
                    {
                        switch (op)
                        {
                            case OperationType.MulDiv:
                                for (int i = 0; i < count; i++) { bufy[ch][i] *= tempbuf[srcch][i]; }
                                break;
                            case OperationType.Sidechain:
                                for (int i = 0; i < count; i++) { bufy[ch][i] *= tempbuf[srcch][i]; }
                                break;
                            case OperationType.AddSub:
                            default:
                                for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] -= tempbuf[srcch][i]; }
                                break;
                        }
                    }
                }
            }

            switch (op)
            {
                case OperationType.MulDiv:
                    for (int ch = 0; ch < outChCnt; ch++)
                    {
                        for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += Math.Max(Math.Min(bufx[ch][i] / bufy[ch][i], 1.0f), -1.0f); }
                    }
                    break;
                case OperationType.Sidechain:
                    for (int ch = 0; ch < outChCnt; ch++)
                    {
                        for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += bufx[ch][i] * bufy[ch][i]; }
                    }
                    break;
                case OperationType.AddSub:
                default:
                    break;
            }
        }
    }
}
