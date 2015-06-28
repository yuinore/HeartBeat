using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public static class ModuleList
    {
        public class ModuleInfo
        {
            public int Id;
            public string Name;
            public string NameLowerCase;
            public Func<Cell> Generator;

            public ModuleInfo(int id, string name, Func<Cell> generator)
            {
                Id = id;
                Name = name;
                NameLowerCase = name.ToLower();
                Generator = generator;
            }
        }

        public static ModuleInfo[] Modules = new ModuleInfo[]
        {
            new ModuleInfo(0, "Null", () => new NullCell()),
            new ModuleInfo(1, "Analog Filter", () => new BiquadFilter()),
            new ModuleInfo(2, "Analog Osc", () => new AnalogOscillator()),
            new ModuleInfo(3, "ADSR", () => new ADSR()),
            new ModuleInfo(4, "Rainbow", () => new Rainbow()),
            new ModuleInfo(5, "Arithmetic", () => new Arithmetic()),
            new ModuleInfo(6, "Dynamics", () => new Shaper()),
            new ModuleInfo(7, "Comb Filter", () => new CombFilter()),
            new ModuleInfo(8, "Chorus", () => new Chorus()),
            new ModuleInfo(9, "Phase Mod", () => new PhaseModulation()),
            new ModuleInfo(10, "Freq Mod", () => new FrequencyModulation()),
            new ModuleInfo(11, "Const", () => new ConstantCell()),
            new ModuleInfo(12, "Tiny Mixer", () => new TinyMixer()),
            new ModuleInfo(13, "Mic", () => new AudioSource("micinput"))
        };
    }
}
