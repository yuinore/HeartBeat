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

                // Pitch, Amp, Type, OP1
                osc1.AssignControllers(new float[] { 0, 0.03f, (float)Waveform.Saw, 0 });

                var sig5 = rainbow.Generate().Take(100000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(60, 100000),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test1.wav"), sig5.Select(x => x.ToArray()).ToArray(), sig5.Length, 44100, 32);
            }

            {
                var filt2 = new CellTree(() => new ButterworthFilterCell());
                var rainbow = new CellTree(() => new Rainbow());
                var osc1 = new CellTree(() => new AnalogOscillator());
                var osc2 = new CellTree(() => new AnalogOscillator());
                var fenv = new CellTree(() => new ADSR());
                var aenv = new CellTree(() => new ADSR());

                // 順序は問わない
                aenv.AssignChildren(new[] { filt2 });
                filt2.AssignChildren(new[] { rainbow, fenv });
                rainbow.AssignChildren(new[] { osc2 });
                osc2.AssignChildren(new[] { osc1 });

                osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Saw, 0 });
                osc2.AssignControllers(new float[] { -12, 0.05f, (float)Waveform.Saw, 0 });
                aenv.AssignControllers(new float[] { 0.01f, 2, 0, 0.01f });

                var sig5 = aenv.Generate().Take(100000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(60, 100000),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test2.wav"), sig5.Select(x => x.ToArray()).ToArray(), sig5.Length, 44100, 32);
            }

            {
                var lenv = new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 2000000),
                    Pitch = new ExactSignal(Enumerable.Range(0, 2000000).Select(i => (float)(-12 + i / 10000.0)).ToArray()),
                    Locals = null
                };

                {
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Saw, 0 });
                    var sig5 = osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_saw.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    var filt = new CellTree(() => new ButterworthFilterCell());
                    filt.AssignChildren(new[] { osc1 });
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Saw, 0 });
                    var sig5 = filt.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_saw_lp.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Square, 0 });
                    var sig5 = osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_squ.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Tri, 0 });
                    var sig5 = osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_tri.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Sin, 0 });
                    var sig5 = osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_sin.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Impulse, 0 });
                    var sig5 = osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_impulse.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
            }

            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test3.wav"), new float[][] { Enumerable.Range(0, 1048576).Select(x => (float)Math.Sin(8 * Math.PI * x / 1048576)).ToArray() }, 1, 44100, 32);
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test4.wav"), new float[][] { Enumerable.Range(0, 1048576).Select(x => (float)FastMath.Sin(8 * Math.PI * x / 1048576)).ToArray() }, 1, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stopwatch s = new Stopwatch();
            s.Start();

            string json = System.IO.File.ReadAllText(HatoPath.FromAppDir("patch.txt"));

            PatchReader pr = new PatchReader(json);


            var lenv = new LocalEnvironment
            {
                SamplingRate = 44100,
                Freq = new ConstantSignal(441, 200000),
                Pitch = new ConstantSignal(60, 200000),
                Locals = null
            };

            var sig = pr.Root.Generate().Take(200000, lenv);

            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("new_from_patch.wav"), sig.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }
    }
}
