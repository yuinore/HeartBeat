using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class AudioSource : InputThroughCell
    {
        readonly string varName;

        public AudioSource(string sourcename)
        {
            varName = sourcename;
        }

        public override int ChannelCountInternal
        {
            get { return 1; }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                };
            }
        }

        public override void TakeInternal(int count, LocalEnvironment lenv)
        {
            Signal sig;

            // TODO: varName が pitch などであった場合の処理
            if (lenv.Locals.TryGetValue(varName, out sig) == false)
            {
                sig = new ConstantSignal(0, count);
            }

            float[] buf = sig.ToArray();

            for (int i = 0; i < count; i++)
            {
                lenv.Buffer[0][i] = buf[i];
            }
        }
    }
}
