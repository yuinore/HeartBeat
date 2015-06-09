using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class ConstantCell : SingleInputCell
    {
        float val = 1.0f;

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1)
            {
                val = ctrl[0].Value;
            }
        }

        public override CellParameterInfo[] ParamsList
        {
            get {
                return new CellParameterInfo[] {
                    new CellParameterInfo("value", true, -2.0f, 2.0f, 1.0f, CellParameterInfo.IdLabel)
                };
            }
        }

        public override int ChannelCount
        {
            get { return 1; }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            float[] buf = lenv.Buffer[0];

            for (int i = 0; i < count; i++)
            {
                buf[i] += val;
            }
        }
    }
}
