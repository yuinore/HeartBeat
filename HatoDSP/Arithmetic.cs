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
        }

        Cell[] child = new Cell[] { };
        int[] port;

        public override CellParameter[] ParamsList
        {
            get { return new CellParameter[] { }; }
        }

        public override void AssignChildren(CellWire[] children)
        {
            this.child = children.Select(x => x.Source.Generate()).ToArray();
            this.port = children.Select(x => x.Port).ToArray();
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            return;
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

            float[][] outbuf = lenv.Buffer;

            foreach (var cel in child)
            {
                if (cel.ChannelCount == 1)
                {
                    float[][] tempbuf = new float[1][] { new float[count] };

                    // FIXME: LocalEnvironmentのクローン
                    lenv.Buffer = tempbuf;

                    cel.Take(count, lenv);

                    for(int ch = 0; ch < outChCnt; ch++)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            outbuf[ch][i] += tempbuf[0][i];
                        }
                    }
                }
                else
                {
                    // TODO: チャンネル数が異なるときエラー

                    lenv.Buffer = outbuf;

                    cel.Take(count, lenv);
                }
            }

            lenv.Buffer = outbuf;
        }
    }
}
