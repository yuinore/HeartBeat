using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSPLib = HatoDSPFast;  // 実行時間に差は無し、プロファイラのサンプリング数では22%の高速化。他も移植したらもう少し速くなりそうだけれどそれはまたいつか

namespace HatoDSP
{
    public class AnalogOscillator : SingleInputCell
    {
        // Cell[] base.InputCells;

        Cell cell
        {
            get {
                Cell x = base.InputCells[0];
                return (x is NullCell) ? null : x;  // NullCellに対してTakeをすることを防ぐ
            }
        }

        CellParameterValue[] ctrl;

        HatoDSPFast.FastOscillator fastOsc;

        Waveform waveform = Waveform.Saw;

        public AnalogOscillator()
        {
            fastOsc = new HatoDSPFast.FastOscillator();
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            this.ctrl = ctrl;
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                // GUIに関しては、もう少しいろいろしないといけない感じはしますね・・・
                // 何ていうか、工夫が必要だと思います    
                return new CellParameterInfo[]{
                    new CellParameterInfo("Pitch", true, -60, 60, 0, x => (x * 100) + "cents"),
                    new CellParameterInfo("Amp", true, 0, 1, 0.5f, x => (x * 100) + "%"),
                    new CellParameterInfo("Type", true, 0, (int)Waveform.Count - 1, (int)Waveform.Saw, x => ((Waveform)(int)(x + 0.5)).ToString()),  // 例外？
                    new CellParameterInfo("PW", true, 0, 1, 0.125f, x => (x * 100) + "%")
                };
            }
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

        float[] buf = new float[256];

        public override void Skip(int count, LocalEnvironment lenv)
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
                if (ctrl.Length >= 3) { waveform = (Waveform)(int)(ctrl[2].Value + 0.5); }
                if (ctrl.Length >= 4) { op1 = ctrl[3].Value; }
            }

            float[] phaseShiftArr = null;

            if (lenv.Locals.ContainsKey("phase"))
            {
                phaseShiftArr = lenv.Locals["phase"].ToArray();
            }

            bool constantPitch = (lenv.Pitch is ConstantSignal);  // メモ：Expressionが導入された場合に修正
            
            if (cell != null)
            {
                if (buf.Length < count)
                {
                    buf = new float[count];
                }

                fastOsc.Skip(
                    count, buf,
                    pshift, amp, (int)waveform, op1,
                    lenv.SamplingRate,
                    constantPitch,
                    constantPitch ? ((ConstantSignal)lenv.Pitch).val : 0,
                    constantPitch ? null : lenv.Pitch.ToArray(),
                    phaseShiftArr != null,
                    phaseShiftArr);

                cell.Skip(count, lenv);  // バッファに加算
            }
            else
            {
                fastOsc.Skip(
                    count, lenv.Buffer[0],
                    pshift, amp, (int)waveform, op1,
                    lenv.SamplingRate,
                    constantPitch,
                    constantPitch ? ((ConstantSignal)lenv.Pitch).val : 0,
                    constantPitch ? null : lenv.Pitch.ToArray(),
                    phaseShiftArr != null,
                    phaseShiftArr);  // 結果を格納
            }
        }

        public override void Take(int count, LocalEnvironment lenv)
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
                if (ctrl.Length >= 3) { waveform = (Waveform)(int)(ctrl[2].Value + 0.5); }
                if (ctrl.Length >= 4) { op1 = ctrl[3].Value; }
            }

            float[] phaseShiftArr = null;

            if (lenv.Locals.ContainsKey("phase"))
            {
                phaseShiftArr = lenv.Locals["phase"].ToArray();
            }

            bool constantPitch = (lenv.Pitch is ConstantSignal);  // メモ：Expressionが導入された場合に修正
            
            if (cell != null)
            {
                if (buf.Length < count)
                {
                    buf = new float[count];
                }

                for (int i = 0; i < count; i++)
                {
                    buf[i] = 0;
                }

                fastOsc.Take(
                    count, buf,
                    pshift, amp, (int)waveform, op1,
                    lenv.SamplingRate,
                    constantPitch,
                    constantPitch ? ((ConstantSignal)lenv.Pitch).val : 0,
                    constantPitch ? null : lenv.Pitch.ToArray(),
                    phaseShiftArr != null,
                    phaseShiftArr);

                cell.Take(count, lenv);  // バッファに加算

                int chCount = cell.ChannelCount;
                for (int ch = 0; ch < chCount; ch++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        lenv.Buffer[ch][i] += buf[i];  // 結果をすべてのチャンネルに格納
                    }
                }
            }
            else
            {
                fastOsc.Take(
                    count, lenv.Buffer[0],
                    pshift, amp, (int)waveform, op1,
                    lenv.SamplingRate,
                    constantPitch,
                    constantPitch ? ((ConstantSignal)lenv.Pitch).val : 0,
                    constantPitch ? null : lenv.Pitch.ToArray(),
                    phaseShiftArr != null,
                    phaseShiftArr);  // 結果を格納
            }
        }
    }
}
