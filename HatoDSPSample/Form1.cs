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
                var rainbow = new Rainbow();
                rainbow.AssignChildren(new[] { new CellTree(() => new AnalogOscillator()) });
                var sig5 = rainbow.Take(100000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(60, 100000),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test1.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
            }

            {
                var filt2 = new AnalogFilter();
                filt2.AssignChildren(new[] {
                    new CellTree(() =>
                    {
                       var cell = new Rainbow();
                       cell.AssignChildren(new[]
                       { 
                           new CellTree(() =>
                           {
                               return new AnalogOscillator();
                           })
                       });
                       return cell;
                    }),
                    new CellTree(() =>
                    {
                        return new ADSR();
                    })
                });
                var sig5 = filt2.Take(100000, new LocalEnvironment
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
                var sig5 = filt2.Take(100000, new LocalEnvironment
                {
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(60, 100000),
                    Locals = null
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test5.wav"), sig5.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
            }

            //WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test3.wav"), new float[][] { Enumerable.Range(0, 100000).Select(x => (float)Math.Sin(8 * Math.PI * x * 0.00001)).ToArray() }, 1, 44100, 32);
            //WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test4.wav"), new float[][] { Enumerable.Range(0, 100000).Select(x => FastMath.Sin(8 * Math.PI * x * 0.00001)).ToArray() }, 1, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }
    }
}
