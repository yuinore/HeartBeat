using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class AnalogOscillator : Cell
    {
        CellTree child0;
        Cell cell;
        Controller[] ctrl;

        Waveform waveform = Waveform.Saw;

        const int MAX_OVERTONE = 100;
        float[] int_inv;
        double old = 0;
        int i2 = 0;

        double phase = 0;  // 積分を行うような場合には精度を必要とする

        public AnalogOscillator()
        {
            int_inv = Enumerable.Range(0, MAX_OVERTONE).Select(x => x == 0 ? 1f : (float)(1.0 / x)).ToArray();
        }

        public override void AssignChildren(CellTree[] children)
        {
            this.child0 = children[0];
            cell = child0.Generate();
        }

        public override void AssignControllers(Controller[] ctrl)
        {
            this.ctrl = ctrl;
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            float pshift = 0;
            float amp = 1;
            float op1 = 0;

            // ctrlの解釈
            // Pitch, Amp, Type, OP1
            if (ctrl != null)
            {
                if (ctrl.Length >= 1) { pshift = ctrl[0].Value; }
                if (ctrl.Length >= 2) { amp = ctrl[1].Value; }
                if (ctrl.Length >= 3) { waveform = (Waveform)ctrl[2].Value; }
                if (ctrl.Length >= 4) { op1 = ctrl[3].Value; }
            }

            // 処理
            float[] ret = new float[count];

            float[] pitch = lenv.Pitch.ToArray();  // TODO:ConstantSignalの場合の最適化

            float _1_rate = 1.0f / lenv.SamplingRate;

            double log2_100 = Math.Log(100) / Math.Log(2);  // FIXME: サンプリングレートが44k以外のとき

            Func<double, int, double> generator = FastMath.Saw;

            switch (waveform)
            {
                case Waveform.Saw:
                    for (int i = 0; i < count; i++, i2++)
                    {
                        double freq = Math.Pow(2, (pitch[i] + pshift - 60.0) / 12.0) * 441;
                        double phasedelta = (2 * Math.PI * freq * _1_rate);
                        int logovertone = (int)((60.0 - pitch[i]) / 12.0 + log2_100 - 1);

                        if (logovertone < 8)
                        {
                            /*ret[i] = 0;
                            int n2 = 1;
                            for (double n = phasedelta; n < Math.PI / 4 && n2 < MAX_OVERTONE; n += phasedelta, n2 ++)
                            {
                                ret[i] += (float)(FastMath.Sin(n2 * phase) * 0.01 * int_inv[n2] * int_inv[n2]);
                            }*/
                            if (phasedelta >= Math.PI)
                            {
                                ret[i] = 0;
                            }
                            else
                            {
                                ret[i] = (float)(FastMath.Saw(phase, logovertone) * amp);
                            }
                            /*
                            double P = lenv.SamplingRate / freq;
                            double M = Math.Floor((P + 1) / 2) * 2 - 1;
                            old = old + FastMath.Sin(Math.PI * M / P * (i2 + 1e-1)) / (FastMath.Sin(Math.PI / P * (i2 + 1e-1)) * P) - 1 / P;  // TODO:ゼロ除算
                            ret[i] = (float)((old - 0.5) * 0.01);
                             */
                        }
                        else
                        {
                            ret[i] += (float)((0.5 - (phase / (2 * Math.PI)) % 1) * 2 * amp);
                        }
                        phase += phasedelta;
                    }
                    break;

                case Waveform.Square:
                    for (int i = 0; i < count; i++, i2++)
                    {
                        double freq = Math.Pow(2, (pitch[i] + pshift - 60.0) / 12.0) * 441;
                        double phasedelta = (2 * Math.PI * freq * _1_rate);
                        int logovertone = (int)((60.0 - pitch[i]) / 12.0 + log2_100 - 1);

                        if (logovertone < 8)
                        {
                            if (phasedelta >= Math.PI)
                            {
                                ret[i] = 0;
                            }
                            else
                            {
                                ret[i] = (float)((FastMath.Saw(phase, logovertone) - FastMath.Saw(phase + Math.PI, logovertone)) * amp);
                            }
                        }
                        else
                        {
                            ret[i] += (float)((int)(phase / Math.PI) % 2 == 0 ? amp : -amp);
                        }

                        phase += phasedelta;
                    }
                    break;

                case Waveform.Tri:
                    for (int i = 0; i < count; i++, i2++)
                    {
                        double freq = Math.Pow(2, (pitch[i] + pshift - 60.0) / 12.0) * 441;
                        double phasedelta = (2 * Math.PI * freq * _1_rate);
                        int logovertone = (int)((60.0 - pitch[i]) / 12.0 + log2_100 - 1);

                        if (phasedelta >= Math.PI)
                        {
                            ret[i] = 0;
                        }
                        else
                        {
                            ret[i] = (float)(FastMath.Tri(phase, logovertone) * amp);
                        }

                        phase += phasedelta;
                    }
                    break;
                default:
                    for (int i = 0; i < count; i++)
                    {
                        double freq = Math.Pow(2, (pitch[i] + pshift - 60.0) / 12.0) * 441;
                        double phasedelta = (2 * Math.PI * freq * _1_rate);

                        if (phasedelta >= Math.PI)
                        {
                            ret[i] = 0;
                        }
                        else
                        {
                            ret[i] = (float)(FastMath.Sin(phase) * amp);
                        }

                        phase += phasedelta;
                    }
                    break;
            }

            if (child0 != null)
            {
                var src = cell.Take(count, lenv);
                return src.Select(x => Signal.Add(x, new ExactSignal(ret, 1.0f, false))).ToArray();  // チャンネル数は入力信号と同じ
            }
            else
            {
                return new[] { new ExactSignal(ret, 1.0f, false) };  // チャンネル数は1
            }
        }
    }
}
