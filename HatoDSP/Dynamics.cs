﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class Dynamics : SingleInputCell
    {
        float inv_drive = 0.01f;
        JovialBuffer jBuf = new JovialBuffer();

        Cell child
        {
            get { return base.InputCells[0]; }
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                    new CellParameterInfo("Drive", true, 0, 1000, 100, CellParameterInfo.IdLabel)
                };
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1) { inv_drive = 1f / ctrl[0].Value; }
        }

        // メモ：入力を自動で加算し、1in-1out演算ができるようなアレ

        public override int ChannelCount
        {
            get
            {
                return child.ChannelCount;
            }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            int outChCnt = child.ChannelCount;

            LocalEnvironment lenv2 = lenv.Clone();
            float[][] tempbuf = jBuf.GetReference(outChCnt, count);
            lenv2.Buffer = tempbuf;

            child.Take(count, lenv2);

            for (int ch = 0; ch < outChCnt; ch++)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = tempbuf[ch][i];

                    if (x > inv_drive) x = inv_drive;  // Hard Clip
                    if (x < -inv_drive) x = -inv_drive;

                    lenv.Buffer[ch][i] += x;
                }
            }
        }
    }
}