﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public abstract class Module  // 一般のシステムを表す
    {
        public abstract Signal[] Take(int count, params Signal[][] input);
    }
}
