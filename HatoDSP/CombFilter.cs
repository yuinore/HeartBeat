using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class CombFilter : Cell
    {
        Cell child = new NullCell();

        public override CellParameter[] ParamsList
        {
            get
            {
                return new CellParameter[] {
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

        public override int ChannelCount
        {
            get
            {
                return child.ChannelCount;
            }
        }

        float a1 = 0.90f;
        float b0 = 1.0f;
        float b1 = 0.0f;
        float delaySamples = 300;
        const int maxDelaySamples = 1024;
        float[][] delayBuffer;
        int j = 0;
        int j0 = 0;  // 現在のdelayBufferの位置

        public override void Take(int count, LocalEnvironment lenv)
        {
            int outChCnt = child.ChannelCount;

            LocalEnvironment lenv2 = lenv.Clone();

            if (delayBuffer == null || delayBuffer.Length < outChCnt)
            {
                delayBuffer = (new int[outChCnt]).Select(x => new float[maxDelaySamples]).ToArray();
            }

            float[][] input = new float[outChCnt][];

            for (int ch = 0; ch < outChCnt; ch++)
            {
                input[ch] = new float[count];
            }

            lenv2.Buffer = input;

            child.Take(count, lenv2);

            for (int i = 0; i < count; i++)
            {
                for (int ch = 0; ch < outChCnt; ch++)
                {
                    // delaySamplesの設定(デバッグ用)
                    // delaySamples = 100.0f + 50 * (float)Math.Sin(j * 0.0004 + Math.PI * ch * 0.5);

                    Debug.Assert(delaySamples >= 0 && delaySamples < maxDelaySamples);

                    int j1 = j0 - (int)delaySamples + maxDelaySamples;  // delaySamplesは変数
                    if (j1 >= maxDelaySamples) j1 -= maxDelaySamples;

                    float t1 = delayBuffer[ch][j1];

                    float t0 = input[ch][i] - a1 * t1;
                    if (-1.1754944e-38 < t0 && t0 < 1.1754944e-38) { t0 = 0; }
                
                    delayBuffer[ch][j0] = t0;
                    input[ch][i] = t0 * b0 + t1 * b1;

                    lenv.Buffer[ch][i] += input[ch][i];  // 出力
                }

                j++;
                if (++j0 >= maxDelaySamples) j0 -= maxDelaySamples;
            }
        }
    }
}
