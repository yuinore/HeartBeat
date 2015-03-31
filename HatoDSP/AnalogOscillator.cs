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
            double overtoneBias = 0.36;  // 調整値

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

            double temp = Math.Log((lenv.SamplingRate * 0.5) / 441.0) / Math.Log(2);  // log2((SR * 0.5) / 441)
            double temp2 = 1.0 / ((lenv.SamplingRate * 0.5) / 441.0);

            Func<double, int, double> generator = FastMath.Saw;

            for (int i = 0; i < count; i++, i2++)
            {
                double freqoctave = (pitch[i] + pshift - 60.0) / 12.0;  // 441HzのAの音からのオクターブ差[oct]
                double freqratio = Math.Pow(2, freqoctave);             // 441HzのAの音からの音声の周波数比
                double freq = freqratio * 441;                          // 音声の周波数[Hz]
                double phasedelta = (2 * Math.PI * freq * _1_rate);     // 音声の角周波数；基音の位相の増分[rad]
                double logovertonefloat = temp - freqoctave + overtoneBias;  // 倍音(基音を含む)の数の、底を2とする対数
                // (*注：正確には、「小数部分切り捨てると、基音を含む倍音の数になる数字」の、底を2とする対数)
                int logovertone = (int)logovertonefloat;                // 倍音(基音を含む)の数の、底を2とする対数を切り捨てた数

                switch (waveform)
                {
                    case Waveform.Saw:
                        if (logovertone < 8)
                        {
                            if (phasedelta >= Math.PI) { ret[i] = 0; }
                            else { ret[i] = (float)(FastMath.Saw(phase, logovertone)); }
                        }
                        else
                        {
                            ret[i] += (float)((0.5 - (phase / (2 * Math.PI)) % 1) * 2);
                        }
                        break;

                    case Waveform.Square:
                        if (logovertone < 8)
                        {
                            if (phasedelta >= Math.PI) { ret[i] = 0; }
                            else { ret[i] = (float)((FastMath.Saw(phase, logovertone) - FastMath.Saw(phase + Math.PI, logovertone))); }
                        }
                        else
                        {
                            ret[i] += (float)((int)(phase / Math.PI) % 2 == 0 ? 1 : -1);
                        }
                        break;

                    case Waveform.Tri:
                        if (phasedelta >= Math.PI) { ret[i] = 0; }
                        else
                        {
                            ret[i] = (float)(FastMath.Tri(phase, logovertone));
                        }
                        break;

                    case Waveform.Impulse:
                        if (logovertone < 8)
                        {
                            if (phasedelta >= Math.PI) { ret[i] = 0; }
                            else
                            {
                                double invovertonecount = freqratio * temp2;  // 小数部分切り捨てると、基音を含む倍音の数になる数字の逆数
                                ret[i] = (float)(FastMath.Impulse(phase, logovertone) * invovertonecount * 10 / Math.PI);  // 音量はLPFを通した後基準で
                            }
                        }
                        else
                        {
                            ret[i] += (float)((int)((phase - phasedelta) / Math.PI) % 2 == 1 && (int)(phase / Math.PI) % 2 == 0 ? 2 : 0);
                        }
                        break;

                    default:
                        if (phasedelta >= Math.PI)
                        {
                            ret[i] = 0;
                        }
                        else
                        {
                            ret[i] = (float)FastMath.Sin(phase);
                        }
                        break;
                }

                phase += phasedelta;
            }

            if (child0 != null)
            {
                var src = cell.Take(count, lenv);
                return src.Select(x => Signal.Add(x, new ExactSignal(ret, amp, false))).ToArray();  // チャンネル数は入力信号と同じ
            }
            else
            {
                return new[] { new ExactSignal(ret, amp, false) };  // チャンネル数は1
            }
        }
    }
}
