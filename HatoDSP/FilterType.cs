using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public enum FilterType
    {
        None = 0,
        AllPass = 0,
        LowPass,
        HighPass,
        BandPass,
        Notch,
        LowShelf,
        HiShelf,
        Peaking
    }
}
