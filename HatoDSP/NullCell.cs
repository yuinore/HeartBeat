﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    internal class NullCell : Cell
    {
        public override CellParameterInfo[] ParamsList
        {
            get { return new CellParameterInfo[] { }; }
        }

        public override void AssignChildren(CellWire[] children)
        {
            return;
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            return;
        }

        public override int ChannelCount
        {
            get { return 1; }
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            return;
        }
    }
}
