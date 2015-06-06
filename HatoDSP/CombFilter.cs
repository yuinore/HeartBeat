using HatoDSPFast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class CombFilter : SingleInputCell
    {
        // [NotNull]
        Cell child
        {
            get
            {
                return InputCells[0];
            }
        }

        // [CanBeNull]
        Cell child2
        {
            get
            {
                if (base.InputCells.Length <= 1 || base.InputCells[1] is NullCell)
                {
                    return null;
                }
                return base.InputCells[0];
            }
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                };
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

        float delayTimeMs = 12.0f;

        float a1 = -0.75f;  // feedback信号. a1 < 0 のとき、低周波帯域が共振する。（強調される）
        float b0 = 1.0f;  // dry(through)信号
        float b1 = 0.0f;  // delayed信号
        float delaySamples = 300;
        readonly int maxDelaySamples = 65536;
        JovialBuffer jDelayBuffer = new JovialBuffer();
        JovialBuffer jInput = new JovialBuffer();
        JovialBuffer jSidechain = new JovialBuffer();
        int j0 = 0;  // 現在のdelayBufferの位置

        public override void Take(int count, LocalEnvironment lenv)
        {
            int outChCnt = child.ChannelCount;

            float[][] delayBuffer = jDelayBuffer.GetReference(outChCnt, maxDelaySamples);  // 注：countではない

            LocalEnvironment lenv2 = lenv.Clone();
            float[][] input = jInput.GetReference(outChCnt, count);
            lenv2.Buffer = input;
            child.Take(count, lenv2);

            float[][] sidechain = null;
            if (child2 != null)
            {
                sidechain = jSidechain.GetReference(outChCnt, count);
                lenv2.Buffer = sidechain;
                child2.Take(count, lenv2);
            }

            for (int i = 0; i < count; i++)
            {
                for (int ch = 0; ch < outChCnt; ch++)
                {
                    delaySamples = delayTimeMs * lenv.SamplingRate * 1e-3f;
                    if (sidechain != null)
                    {
                        delaySamples += sidechain[ch][i] * 100;
                    }
                    if (delaySamples > maxDelaySamples - 1) delaySamples = maxDelaySamples - 1;
                    if (delaySamples < 0) delaySamples = 0;

                    Debug.Assert(delaySamples >= 0 && delaySamples < maxDelaySamples);

                    int j1 = j0 - (int)delaySamples + maxDelaySamples;  // delaySamplesは変数
                    if (j1 >= maxDelaySamples) j1 -= maxDelaySamples;

                    float t1 = delayBuffer[ch][j1];  // TODO:線形補間

                    float t0 = input[ch][i] - a1 * t1;
                    if (-1.1754944e-38 < t0 && t0 < 1.1754944e-38) { t0 = 0; }

                    delayBuffer[ch][j0] = t0;
                    float result = t0 * b0 + t1 * b1;

                    lenv.Buffer[ch][i] += result;  // 出力

                    //input[ch][i] = result;  // 無意味？
                }

                if (++j0 >= maxDelaySamples) j0 -= maxDelaySamples;
            }
        }
    }
}
