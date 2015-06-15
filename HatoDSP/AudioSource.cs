using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class AudioSource : SingleInputCell
    {
        string varName;

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            throw new NotImplementedException();
        }

        public override CellParameterInfo[] ParamsList
        {
            get { throw new NotImplementedException(); }
        }

        public override int ChannelCount
        {
            get { throw new NotImplementedException(); }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            throw new NotImplementedException();
        }
    }
}
