using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    /// <summary>
    /// Chorus, Delay, Flanger を実装します。Comb Filterとほぼ同一です。
    /// </summary>
    class Chorus : SingleInputCell
    {
        Cell child
        {
            get
            {
                return InputCells[0];
            }
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                    new CellParameterInfo("Dry",        true,  0.00f, 1.0f,  1.0f, CellParameterInfo.PercentLabel),
                    new CellParameterInfo("Delayed",    true, -1.00f, 1.0f,  1.0f, CellParameterInfo.PercentLabel),
                    new CellParameterInfo("Feedback",   true, -0.99f, 0.99f, 0.0f, CellParameterInfo.PercentLabel),
                    new CellParameterInfo("LFO Amount", true,  0.0f, 100.0f, 20.0f, x => x + ""),
                    new CellParameterInfo("LFO Freq", true,  0.0f, 100.0f, 0.5f, x => x + "Hz"),
                    new CellParameterInfo("Delay Time", true,  0.0f, 1000.0f, 12.0f, x => x + "ms")
                };
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1) { b0 = ctrl[0].Value; }
            if (ctrl.Length >= 2) { b1 = ctrl[1].Value; }
            if (ctrl.Length >= 3) { a1 = Math.Max(Math.Min(ctrl[2].Value, 0.99f), -0.99f); }
            if (ctrl.Length >= 4) { LFOAmount = ctrl[3].Value; }
            if (ctrl.Length >= 5) { LFOFrequency = ctrl[4].Value; }
            if (ctrl.Length >= 6) { delayTimeMs = ctrl[5].Value; }
        }

        public override int ChannelCount
        {
            get
            {
                return child.ChannelCount;
            }
        }

        float LFOAmount = 80;
        float LFOFrequency = 0.5f;  // Hz
        float LFOPhase = 0;
        float LFOStereoPhase = (float)Math.PI * 0.5f;
        float delayTimeMs = 12.0f;

        float a1 = 0.0f;  // feedback信号
        float b0 = 1.0f;  // dry(through)信号
        float b1 = 0.9f;  // delayed信号
        float delaySamples = 300;
        readonly int maxDelaySamples = 65536;
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
                    delaySamples = delayTimeMs * lenv.SamplingRate / 1000.0f + (LFOAmount / LFOFrequency) * (float)Math.Sin(LFOPhase + LFOStereoPhase * ch);
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

                LFOPhase += 2 * (float)Math.PI * LFOFrequency / lenv.SamplingRate;
                if (LFOPhase > 16 * 2 * Math.PI) LFOPhase -= 16 * (float)Math.PI;

                j++;
                if (++j0 >= maxDelaySamples) j0 -= maxDelaySamples;
            }
        }
    }
}
