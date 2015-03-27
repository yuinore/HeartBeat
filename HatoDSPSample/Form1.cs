using HatoDSP;
using HatoLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoDSPSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            {
                var rainbow = new CellTree(() => new Rainbow());
                var osc1 = new CellTree(() => new AnalogOscillator());

                rainbow.AssignChildren(new[] { osc1 });

                var sig5 = rainbow.Generate().Take(100000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(60, 100000),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test1.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
            }

            {
                var filt2 = new CellTree(() => new AnalogFilter());
                var rainbow = new CellTree(() => new Rainbow());
                var osc1 = new CellTree(() => new AnalogOscillator());
                var adsr = new CellTree(() => new ADSR());

                // 順序は問わない
                filt2.AssignChildren(new[] { rainbow, adsr });
                rainbow.AssignChildren(new[] { osc1 });

                osc1.AssignControllers(new[] { new Controller("Pitch", 0), new Controller("Amp", 0.01f), new Controller("Type", (int)Waveform.Saw), new Controller("OP 1", 0) });

                var sig5 = filt2.Generate().Take(100000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(60, 100000),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test2.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
            }

            {
                var filt2 = new AnalogOscillator();
                var sig5 = filt2.Take(2000000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 2000000),
                    Pitch = new ExactSignal(Enumerable.Range(0, 2000000).Select(i => (float)(-12 + i / 10000.0)).ToArray()),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test5.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
            }

            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test3.wav"), new float[][] { Enumerable.Range(0, 1048576).Select(x => (float)Math.Sin(8 * Math.PI * x / 1048576)).ToArray() }, 1, 44100, 32);
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test4.wav"), new float[][] { Enumerable.Range(0, 1048576).Select(x => (float)FastMath.Sin(8 * Math.PI * x / 1048576)).ToArray() }, 1, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }
    }
}
