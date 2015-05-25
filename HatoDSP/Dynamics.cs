using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class Dynamics : Cell
    {
        Cell child = new NullCell();

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                };
            }
        }

        public override void AssignChildren(CellWire[] children)
        {
            if (children.Length >= 1)
            {
                this.child = children[0].Source.Generate();
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
        }

        // メモ：入力を自動で加算し、1in-1out演算ができるようなアレ

        public override int ChannelCount
        {
            get
            {
                return child.ChannelCount;
            }
        }

        float inv_drive = 0.01f;

        public override void Take(int count, LocalEnvironment lenv)
        {
            int outChCnt = child.ChannelCount;

            LocalEnvironment lenv2 = lenv.Clone();

            float[][] tempbuf = new float[outChCnt][];

            for (int ch = 0; ch < outChCnt; ch++)
            {
                tempbuf[ch] = new float[count];
            }

            lenv2.Buffer = tempbuf;

            child.Take(count, lenv2);

            for (int ch = 0; ch < outChCnt; ch++)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = tempbuf[ch][i];

                    if (x > inv_drive) x = inv_drive;
                    if (x < -inv_drive) x = -inv_drive;

                    lenv.Buffer[ch][i] += x;
                }
            }
        }
    }
}