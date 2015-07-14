using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class ADSR : SingleInputCell
    {
        Cell cell
        {
            get
            {
                Cell x = base.InputCells[0];
                return (x is NullCell) ? null : x;  // NullCellに対してTakeをすることを防ぐ
            }
        }

        CellParameterValue[] ctrl;

        double time = 0;  // 累積時間
        int n = 0;  // 累積サンプル数

        float A = 0.00f;
        float D = 0.5f;
        float S = 0.1f;
        float R = 0.01f;

        float lastgain = 0.0f;
        double releasedAt = 0.0f;
        bool releaseFinished = false;

        JovialBuffer jEnvelope = new JovialBuffer();
        JovialBuffer jBuf2 = new JovialBuffer();

        double log2a = Math.Log(0.00001, 2);  // -100dB (1 / 10^5)

        public ADSR()
        {
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                // TODO: パラメータの対数化
                return new CellParameterInfo[]{
                    new CellParameterInfo("Attack", true, 0, 1, 0.01f, x => x + "s"),
                    new CellParameterInfo("Decay", true, 0, 1, 0.5f, x => x + "s"),
                    new CellParameterInfo("Sustain", true, 0, 1, 0.1f, x => x + "s"),
                    new CellParameterInfo("Release", true, 0, 1, 0.01f, x => x + "s")
                };
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            this.ctrl = ctrl;
        }

        public override int ChannelCount
        {
            get
            {
                if (cell != null)
                {
                    return cell.ChannelCount;
                }
                else
                {
                    return 1;
                }
            }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            if (releaseFinished) return;

            // ctrlの解釈
            // A, D, S, R
            if (ctrl != null)
            {
                if (ctrl.Length >= 1) { A = ctrl[0].Value; }
                if (ctrl.Length >= 2) { D = ctrl[1].Value; }
                if (ctrl.Length >= 3) { S = ctrl[2].Value; }
                if (ctrl.Length >= 4) { R = ctrl[3].Value; }
            }

            float[] gate = lenv.Gate is ConstantSignal ? null : lenv.Gate.ToArray();
            bool gate_lt_05 = lenv.Gate is ConstantSignal && ((ConstantSignal)lenv.Gate).val > 0.5;

            float[] envelope = jEnvelope.GetReference(1, count)[0];  // メモ：0初期化は不要

            double dt = 1.0 / lenv.SamplingRate;

            for (int i = 0; i < count; n++, i++, time += dt)
            {
                if (gate == null ? gate_lt_05 : gate[i] > 0.5)
                {
                    if (time < A)
                    {
                        lastgain = envelope[i] = (float)(time / A);
                    }
                    else if (time < A + D)
                    {
                        //double rate = Math.Pow(0.00001, (time - A) / D);
                        //double rate = FastMath.Pow2(log2a * (time - A) / D); // 0dB to -100dB
                        //lastgain = ret[i] = (float)((1.0f - S) * rate + S);
                        
                        // (0.01)^t * (0.5+0.5*cos(3.1415*t))
                        // ↑エンベロープにはこれが良いと思う
                        double t = (time - A) / D;
                        lastgain = envelope[i] = (float)((1.0f - S) * Math.Pow(0.01, t) * (0.5 + 0.5 * Math.Cos(Math.PI * t)) + S);  // -40dB
                    }
                    else
                    {
                        lastgain = envelope[i] = S;  // TODO: ConstantSignal化
                    }
                    releasedAt = time;
                }
                else
                {
                    if (time < releasedAt + R)
                    {
                        //double rate = Math.Pow(0.00001, (time - releasedAt) / R);
                        double rate = HatoDSPFast.FastMathWrap.Pow2(log2a * (time - releasedAt) / R); // 0dB to -100dB
                        envelope[i] = (float)(lastgain * rate);
                    }
                    else
                    {
                        envelope[i] = 0;  // TODO: ConstantSignal化
                        releaseFinished = true;
                    }
                }

            }

            if (cell != null)
            {
                int carrierChCnt = cell.ChannelCount;

                LocalEnvironment lenv2 = lenv.Clone();
                float[][] buf2 = jBuf2.GetReference(carrierChCnt, count);
                lenv2.Buffer = buf2;  // 別に用意した空のバッファを与える

                cell.Take(count, lenv2);  // バッファに加算

                for (int i = 0; i < count; i++)
                {
                    for (int ch = 0; ch < carrierChCnt; ch++)  // どっちの順序が速い？？
                    {
                        lenv.Buffer[ch][i] += buf2[ch][i] * envelope[i];  // 結果を格納
                    }
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    lenv.Buffer[0][i] += envelope[i];  // 結果を格納
                }
            }
        }
    }
}
