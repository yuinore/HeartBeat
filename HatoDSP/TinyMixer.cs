using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class TinyMixer : SingleInputCell
    {
        float ch1gain = 0.0f;  // dB
        float ch2gain = 0.0f;  // dB
        float balance = 0.5f;  // 0 - 1

        int outChCnt = 0;  // 0 == null

        JovialBuffer jCh1 = new JovialBuffer();
        JovialBuffer jCh2 = new JovialBuffer();

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1) { ch1gain = ctrl[0].Value; }
            if (ctrl.Length >= 2) { ch2gain = ctrl[1].Value; }
            if (ctrl.Length >= 3) { balance = ctrl[2].Value; }
        }

        public override CellParameterInfo[] ParamsList
        {
            get {
                return new CellParameterInfo[] {
                    new CellParameterInfo("Ch 1 Gain", true, -100.0f, 18.0f, 0.0f, CellParameterInfo.IdLabel),
                    new CellParameterInfo("Ch 2 Gain", true, -100.0f, 18.0f, 0.0f, CellParameterInfo.IdLabel),
                    new CellParameterInfo("Ch 1/2 Balance", true, 0.0f, 1.0f, 0.5f, CellParameterInfo.PercentLabel),
                };
            }
        }

        public override int ChannelCount
        {
            get
            {
                UpdateOutChCnt();
                return outChCnt;
            }
        }

        private void UpdateOutChCnt()
        {
            if (outChCnt == 0)
            {
                outChCnt = InputCells.Select(x => x.ChannelCount).Max();
            }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            // TODO: NullCellの場合などの最適化

            UpdateOutChCnt();

            LocalEnvironment lenv2 = lenv.Clone();

            int ch1chCnt = InputCells[0].ChannelCount;
            float[][] ch1 = jCh1.GetReference(ch1chCnt, count);
            lenv2.Buffer = ch1;
            InputCells[0].Take(count, lenv2);
            
            Cell ch2Cell = InputCells.Length >= 2 ? InputCells[1] : new NullCell();
            int ch2chCnt = ch2Cell.ChannelCount;
            float[][] ch2 = jCh2.GetReference(ch2Cell.ChannelCount, count);
            lenv2.Buffer = ch2;
            InputCells[0].Take(count, lenv2);

            float ch1rawgain = SlowMath.DecibelToRaw(ch1gain) * (1 - balance);
            float ch2rawgain = SlowMath.DecibelToRaw(ch2gain) * balance;

            for (int ch = 0; ch < outChCnt; ch++)
            {
                int ch1srcCh = ch1chCnt == 1 ? 0 : ch % ch1chCnt;
                int ch2srcCh = ch2chCnt == 1 ? 0 : ch % ch2chCnt;

                for (int i = 0; i < count; i++)
                {
                    lenv.Buffer[ch][i] += ch1[ch1srcCh][i] * ch1rawgain + ch2[ch2srcCh][i] * ch2rawgain;
                }
            }
        }
    }
}
