using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class LocalEnvironment  // 不変型にしようと思っているのですが(ry
    {
        public Signal Pitch;
        public Signal Freq;
        public Signal Gate;
        public Dictionary<string, Signal> Locals;
        public float SamplingRate;
    }
}
