using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class PhaseModulation : SingleInputCell
    {
        float phaseShift = 0.0f;

        public override CellParameterInfo[] ParamsList
        {
            get {
                return new CellParameterInfo[] {
                    new CellParameterInfo("phase shift", true, 0.0f, 2.0f*(float)Math.PI, 0.0f, CellParameterInfo.IdLabel)
                };
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1)
            {
                phaseShift = ctrl[0].Value;
            }
        }

        public override int ChannelCount
        {
            get { return InputCells[0].ChannelCount; }
        }

        JovialBuffer buf = new JovialBuffer();

        public override void Take(int count, LocalEnvironment lenv)
        {
            if (InputCells.Length >= 2)
            {
                var lenv2 = lenv.Clone();
                int xchainChCnt = InputCells[0].ChannelCount;
                lenv2.Buffer = buf.GetReference(xchainChCnt, count);  // バッファを確保
                InputCells[1].Take(count, lenv2);

                // todo: ステレオ
                Signal phaseSignal = new ExactSignal(lenv2.Buffer[0], 1.0f, false);

                if (lenv.Locals.ContainsKey("phase"))
                {
                    phaseSignal = Signal.Add(lenv.Locals["phase"], phaseSignal);
                }

                if (phaseShift != 0.0f)
                {
                    phaseSignal = Signal.Add(phaseSignal, new ConstantSignal(phaseShift, count));
                }

                var lenv3 = lenv.Clone();
                lenv3.Locals["phase"] = phaseSignal;
                InputCells[0].Take(count, lenv3);
            }
            else
            {
                InputCells[0].Take(count, lenv);
            }
        }
    }
}
