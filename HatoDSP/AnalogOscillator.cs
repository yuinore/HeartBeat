using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSPLib = HatoDSPFast;  // 実行時間に差は無し、プロファイラのサンプリング数では22%の高速化。他も移植したらもう少し速くなりそうだけれどそれはまたいつか

namespace HatoDSP
{
    public class AnalogOscillator : Cell
    {
        CellTree child0;
        Cell cell;
        Controller[] ctrl;

        Waveform waveform = Waveform.Saw;

        int i2 = 0;

        double phase = 0;  // 積分を行うような場合には精度を必要とする

        public AnalogOscillator()
        {
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

            float[] pitch = null;

            bool constantPitch = (lenv.Pitch is ConstantSignal);  // メモ：Expressionが導入された場合に修正

            double _2pi_rate = 2.0 * Math.PI / lenv.SamplingRate;
            double inv_2pi = 1.0 / (2.0 * Math.PI);
            double inv_pi = 1.0 / Math.PI;
            double _2_pi = 2.0 / Math.PI;
            double inv_12 = 1.0 / 12.0;

            // これらの中間変数自体に物理的な意味は恐らく無いと思います。441は真ん中のラの音(n=69)の周波数です。
            double temp = Math.Log((lenv.SamplingRate * 0.5) / 441.0) / Math.Log(2);  // log_2((SamplingRate * 0.5) / 441)
            double temp2 = 1.0 / ((lenv.SamplingRate * 0.5) / 441.0);

            double freqoctave = 0;
            double freqratio = 0;
            double freq = 0;
            double phasedelta = 0;
            double logovertonefloat = 0;
            int logovertone = 0;
            bool isNotTooLow = false, isTooHigh = false, isVeryHigh = false, isInRange = false;

            if (constantPitch)
            {
                float constpitch = ((ConstantSignal)lenv.Pitch).val;
                freqoctave = (constpitch + pshift - 69.0) * inv_12;    // 441HzのAの音からのオクターブ差[oct]
                freqratio = DSPLib.FastMath.Pow2(freqoctave);          // 441HzのAの音からの音声の周波数比
                freq = freqratio * 441;                                // 音声の周波数[Hz]
                phasedelta = freq * _2pi_rate;                         // 音声の角周波数；基音の位相の増分[rad]
                logovertonefloat = temp - freqoctave + overtoneBias;   // 倍音(基音を含む)の数の、底を2とする対数
                // (*注：正確には、「小数部分切り捨てると、基音を含む倍音の数になる数字」の、底を2とする対数)
                logovertone = (int)logovertonefloat;                   // 倍音(基音を含む)の数の、底を2とする対数を切り捨てた数
                isNotTooLow = logovertone < HatoDSPFast.FastMath.WT_N; // 音が低すぎないかどうかを表すbool変数
                isTooHigh = phasedelta >= Math.PI;                     // 音が高すぎるかどうかを表すbool変数
                isVeryHigh = logovertone <= 0;                         // 音が高く、単一のsin波で信号を表せるかどうかを表す
                isInRange = isNotTooLow && !isVeryHigh;                // 上の3条件をまとめた一時変数
            }
            else
            {
                pitch = lenv.Pitch.ToArray();
            }

            for (int i = 0; i < count; i++, i2++)
            {
                if (!constantPitch)
                {
                    // TODO: Tri, Sinでは使用されない変数があるため最適化
                    freqoctave = (pitch[i] + pshift - 69.0) * inv_12;      // 441HzのAの音からのオクターブ差[oct]
                    freqratio = DSPLib.FastMath.Pow2(freqoctave);          // 441HzのAの音からの音声の周波数比
                    freq = freqratio * 441;                                // 音声の周波数[Hz]
                    phasedelta = freq * _2pi_rate;                         // 音声の角周波数；基音の位相の増分[rad]
                    logovertonefloat = temp - freqoctave + overtoneBias;   // 倍音(基音を含む)の数の、底を2とする対数
                    // (*注：正確には、「小数部分切り捨てると、基音を含む倍音の数になる数字」の、底を2とする対数)
                    logovertone = (int)logovertonefloat;                   // 倍音(基音を含む)の数の、底を2とする対数を切り捨てた数
                    isNotTooLow = logovertone < HatoDSPFast.FastMath.WT_N; // 音が低すぎないかどうかを表すbool変数
                    isTooHigh = phasedelta >= Math.PI;                     // 音が高すぎるかどうかを表すbool変数
                    isVeryHigh = logovertone <= 0;                         // 音が高く、単一のsin波で信号を表せるかどうかを表す
                    isInRange = isNotTooLow && !isVeryHigh;                // 上の3条件をまとめた一時変数
                }

                switch (waveform)
                {
                    case Waveform.Saw:
                        if (isInRange) { ret[i] = (float)(DSPLib.FastMath.Saw(phase, logovertone)); }
                        else if (isTooHigh) { ret[i] = 0; }
                        else if (isVeryHigh) { ret[i] = (float)(DSPLib.FastMath.Sin(phase) * _2_pi); }  // この周波数帯は、出力前LPF(名前忘れた)を掛けると消えてしまう。
                        else
                        {
                            double normphase = phase * inv_2pi;
                            //ret[i] += (float)((0.5 - normphase % 1) * 2);  // FIXME: phaseが負の時の処理
                            double temp3 = normphase - (Int64)normphase;  // 剰余演算。"temp3 = normphase % 1;" を表す。
                            ret[i] += (float)((0.5 - temp3) * 2);  // FIXME: phaseが負の時の処理
                        }
                        break;

                    case Waveform.Square:
                        if (isInRange) { ret[i] = (float)((DSPLib.FastMath.Saw(phase, logovertone) - DSPLib.FastMath.Saw(phase + Math.PI, logovertone))); }
                        else if (isTooHigh) { ret[i] = 0; }
                        else if (isVeryHigh) { ret[i] = (float)(DSPLib.FastMath.Sin(phase) * _2_pi * 2); }
                        else
                        {
                            ret[i] += (float)(((Int64)(phase * inv_pi) & 1) * (-2) + 1);  // (phase * inv_pi) % 2 == 0 ? 1 : -1
                            // FIXME: phaseが負の時の処理
                        }
                        break;

                    case Waveform.Pulse:
                        if (isInRange) { ret[i] = (float)((DSPLib.FastMath.Saw(phase, logovertone) - DSPLib.FastMath.Saw(phase + (1 - op1) * (2 * Math.PI), logovertone))); }
                        else if (isTooHigh) { ret[i] = 0; }
                        else if (isVeryHigh) { ret[i] = (float)((DSPLib.FastMath.Sin(phase) - DSPLib.FastMath.Sin(phase + (1 - op1) * (2 * Math.PI))) * _2_pi); }
                        else
                        {
                            // FIXME: phaseが負の時の処理
                            double x = phase * inv_pi * 0.5;  // 0～1で1周期
                            double decimalPart = x - (Int64)x;  // 小数部分

                            ret[i] += (decimalPart < op1 ? 1 - op1 : -op1) * 2;
                        }
                        break;

                    case Waveform.Tri:
                        if (isInRange) { ret[i] = (float)(DSPLib.FastMath.Tri(phase, logovertone)); }
                        else if (isTooHigh) { ret[i] = 0; }
                        else if (isVeryHigh) { ret[i] = (float)(Math.Cos(phase) * (8 / (Math.PI * Math.PI))); }
                        else
                        {
                            // 不連続点を含まず収束が速いため、最適化を行わない（計算量的には最適化した方が良いかも（要検証））
                            ret[i] = (float)(DSPLib.FastMath.Tri(phase, HatoDSPFast.FastMath.WT_N - 1));  // 第2引数はlogovertoneのままでもよい
                        }
                        break;

                    case Waveform.Impulse:
                        if (isInRange)
                        {
                            double invovertonecount = freqratio * temp2;  // 小数部分切り捨てると、基音を含む倍音の数になる数字の逆数
                            ret[i] = (float)(DSPLib.FastMath.Impulse(phase, logovertone) * invovertonecount * 2);  // 音量はLPFを通した後基準で
                        }
                        else if (isTooHigh) { ret[i] = 0; }
                        else if (isVeryHigh)
                        {
                            double invovertonecount = freqratio * temp2;  // 小数部分切り捨てると、基音を含む倍音の数になる数字の逆数
                            ret[i] = (float)(Math.Cos(phase) * 2 * invovertonecount); 
                        }
                        else
                        {
                            int lastval = (int)((Int64)(phase * inv_pi) & 1L);
                            int currval = (int)((Int64)((phase + phasedelta) * inv_pi) & 1L);

                            ret[i] += (float)((lastval & (1 ^ currval)) << 1);  // lastval == 1 && currentval == 0 ? 2 : 0
                        }
                        break;

                    default:
                        if (isTooHigh)
                        {
                            ret[i] = 0;
                        }
                        else
                        {
                            ret[i] = (float)DSPLib.FastMath.Sin(phase);
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
