using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class ADSR : Cell
    {
        Cell cell;
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

        public override void AssignChildren(CellWire[] children)
        {
            if (children.Length >= 1)
            {
                //this.child0 = children[0].Source;  // FIXME: 複数指定
                cell = children[0].Source.Generate();
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

        float[] ret = new float[256];
        float[][] buf2;

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

            float[] gate = lenv.Gate.ToArray();

            if (ret.Length < count)
            {
                ret = new float[count];  // ゼロ初期化はしない
            }

            double dt = 1.0 / lenv.SamplingRate;

            double log2a = Math.Log(0.00001, 2);  // -100dB (1 / 10^5)

            for (int i = 0; i < count; n++, i++, time += dt)
            {
                if (gate[i] > 0.5)
                {
                    if (time < A)
                    {
                        lastgain = ret[i] = (float)(time / A);
                    }
                    else if (time < A + D)
                    {
                        //double rate = Math.Pow(0.00001, (time - A) / D);
                        //double rate = FastMath.Pow2(log2a * (time - A) / D); // 0dB to -100dB
                        //lastgain = ret[i] = (float)((1.0f - S) * rate + S);
                        
                        // (0.01)^t * (0.5+0.5*cos(3.1415*t))
                        // ↑エンベロープにはこれが良いと思う
                        double t = (time - A) / D;
                        lastgain = ret[i] = (float)((1.0f - S) * Math.Pow(0.01, t) * (0.5 + 0.5 * Math.Cos(Math.PI * t)) + S);  // -40dB
                    }
                    else
                    {
                        lastgain = ret[i] = S;  // TODO: ConstantSignal化
                    }
                    releasedAt = time;
                }
                else
                {
                    if (time < releasedAt + R)
                    {
                        //double rate = Math.Pow(0.00001, (time - releasedAt) / R);
                        double rate = HatoDSPFast.FastMathWrap.Pow2(log2a * (time - releasedAt) / R); // 0dB to -100dB
                        ret[i] = (float)(lastgain * rate);
                    }
                    else
                    {
                        ret[i] = 0;  // TODO: ConstantSignal化
                        releaseFinished = true;
                    }
                }

            }

            if (cell != null)
            {
                if (cell != null && (buf2 == null || buf2.Length < cell.ChannelCount || buf2[0].Length < count))
                {
                    buf2 = (new float[cell.ChannelCount][]).Select(x => new float[count]).ToArray();
                }

                for (int ch = 0; ch < cell.ChannelCount; ch++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        buf2[ch][i] = 0;
                    }
                }

                // TODO: LocalEnvironment.Clone() の実装 (←MemberwiseCloneで良くないですか)
                LocalEnvironment lenv2 = lenv.Clone();
                lenv2.Buffer = buf2;  // 別に用意した空のバッファを与える

                cell.Take(count, lenv2);  // バッファに加算

                int chCount = cell.ChannelCount;
                for (int ch = 0; ch < chCount; ch++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        lenv.Buffer[ch][i] += buf2[ch][i] * ret[i];  // 結果を格納
                    }
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    lenv.Buffer[0][i] += ret[i];  // 結果を格納
                }
            }
        }
    }
}
