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
        readonly double CPU_CLK = 3.4e+9;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            float[][] buf = null;

            while (!HatoDSPFast.FastMathWrap.Initialized)  // 重要
            {
                System.Threading.Thread.Sleep(100);
            }

            Stopwatch s = new Stopwatch();
            s.Start();
            {
                var rainbow = new CellTree(() => new Rainbow());
                var osc1 = new CellTree(() => new AnalogOscillator());

                rainbow.AddChildren(new[] { osc1 });

                // Pitch, Amp, Type, OP1
                osc1.AssignControllers(new float[] { 0, 0.03f, (float)Waveform.Saw, 0 });

                rainbow.Generate().Take(100000, new LocalEnvironment
                {
                    Buffer = buf = new float[2][] { new float[100000], new float[100000] },
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(69, 100000),
                    Gate = new ConstantSignal(1, 100000),
                    Locals = new Dictionary<string,Signal>()
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test1.wav"), buf.Select(x => x.ToArray()).ToArray(), buf.Length, 44100, 32);
            }

            {
                //var filt2 = new CellTree(() => new ButterworthFilterCell());
                var filt2 = new CellTree(() => new BiquadFilter());  // ここ変更あり
                var rainbow = new CellTree(() => new Rainbow());
                var osc1 = new CellTree(() => new AnalogOscillator());
                var osc2 = new CellTree(() => new AnalogOscillator());
                var fenv = new CellTree(() => new ADSR());
                var aenv = new CellTree(() => new ADSR());

                // 順序は問わない
                aenv.AddChildren(new[] { filt2 });
                filt2.AddChildren(new[] { rainbow, fenv });
                rainbow.AddChildren(new[] { osc2 });
                osc2.AddChildren(new[] { osc1 });

                osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Saw, 0 });
                osc2.AssignControllers(new float[] { -12, 0.05f, (float)Waveform.Saw, 0 });
                aenv.AssignControllers(new float[] { 0.01f, 2, 0, 0.01f });

                aenv.Generate().Take(100000, new LocalEnvironment
                {
                    Buffer = buf = new float[2][] { new float[100000], new float[100000] },
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 100000),
                    Pitch = new ConstantSignal(69, 100000),
                    Gate = new ConstantSignal(1, 100000),
                    Locals = new Dictionary<string,Signal>()
                });

                WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test2.wav"), buf.Select(x => x.ToArray()).ToArray(), buf.Length, 44100, 32);
            }

            {
                var lenv = new LocalEnvironment
                {
                    Buffer = null,
                    SamplingRate = 44100,
                    Freq = new ConstantSignal(441, 2000000),
                    Pitch = new ExactSignal(Enumerable.Range(0, 2000000).Select(i => (float)(-3 + i / 10000.0)).ToArray()),
                    Gate = new ConstantSignal(1, 2000000),
                    Locals = new Dictionary<string,Signal>()
                };

                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Saw, 0 });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_saw.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    var filt = new CellTree(() => new BiquadFilter());  // ここ変更あり
                    filt.AddChildren(new[] { osc1 });
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Saw, 0 });
                    filt.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_saw_lp.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Square, 0 });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_squ.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Tri, 0 });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_tri.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Sin, 0 });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_sin.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Impulse, 0 });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_impulse.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Pulse, 0.125f });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_pulse125.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[2000000] };
                    var osc1 = new CellTree(() => new AnalogOscillator());
                    osc1.AssignControllers(new float[] { 0, 0.5f, (float)Waveform.Pulse, 0.25f });
                    osc1.Generate().Take(2000000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_waveform_pulse25.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
                {
                    lenv.Buffer = buf = new float[1][] { new float[80000] };
                    var osc1 = new CellTree(() => new ADSR());
                    osc1.AssignControllers(new float[] { 0.1f, 1.0f, 0.5f, 0.1f });
                    osc1.Generate().Take(80000, lenv);
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test_adsr_1.wav"), buf.Select(x => x.ToArray()).ToArray(), 1, 44100, 32);
                }
            }

            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test3.wav"), new float[][] { Enumerable.Range(0, 1048576).Select(x => (float)Math.Sin(8 * Math.PI * x / 1048576)).ToArray() }, 1, 44100, 32);
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test4.wav"), new float[][] { Enumerable.Range(0, 1048576).Select(x => (float)HatoDSPFast.FastMathWrap.Sin(8 * Math.PI * x / 1048576)).ToArray() }, 1, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            while (!HatoDSPFast.FastMathWrap.Initialized)
            {
                System.Threading.Thread.Sleep(100);
            }

            Stopwatch s = new Stopwatch();
            s.Start();

            string json = System.IO.File.ReadAllText(HatoPath.FromAppDir("patch.txt"));

            PatchReader pr = new PatchReader(json);

            float[][] buf = new float[2][] { new float[200000], new float[200000] };  // ←雑

            var lenv = new LocalEnvironment
            {
                Buffer = buf,
                SamplingRate = 44100,
                Freq = new ConstantSignal(441, 200000),
                Pitch = new ConstantSignal(69, 200000),
                Gate = new ConstantSignal(1, 200000),
                Locals = new Dictionary<string, Signal>()
            };

            pr.Root.Generate().Take(200000, lenv);

            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("new_from_patch.wav"), buf.Select(x => x.ToArray()).ToArray(), buf.Length, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            while (!HatoDSPFast.FastMathWrap.Initialized)
            {
                System.Threading.Thread.Sleep(100);
            }

            Stopwatch s = new Stopwatch();
            s.Start();

            string json = System.IO.File.ReadAllText(HatoPath.FromAppDir("patch.txt"));

            HatoSynthDevice dev = new HatoSynthDevice(json);

            Signal[] sig = dev.Take(20000);

            for (int k = 0; k < 100; k++)
            {
                dev.NoteOn(63 + 9);

                Signal[] sig2 = null;

                for (int j = 0; j < 100; j++)
                {
                    sig2 = dev.Take(200);
                    sig = Enumerable.Range(0, 2).Select(i => Signal.Concat(sig[i], sig2[i])).ToArray();
                }

                for (int j = 0; j < 100; j++)
                {
                    sig2 = dev.Take(200);
                    sig = Enumerable.Range(0, 2).Select(i => Signal.Concat(sig[i], sig2[i])).ToArray();
                }

                dev.NoteOn(67 + 9);

                for (int j = 0; j < 100; j++)
                {
                    sig2 = dev.Take(200);
                    sig = Enumerable.Range(0, 2).Select(i => Signal.Concat(sig[i], sig2[i])).ToArray();
                }

                dev.NoteOff(67 + 9);
                dev.NoteOn(70 + 9);

                sig2 = dev.Take(20000);
                sig = Enumerable.Range(0, 2).Select(i => Signal.Concat(sig[i], sig2[i])).ToArray();

                dev.NoteOff(63 + 9);
                dev.NoteOff(70 + 9);

                for (int j = 0; j < 100; j++)
                {
                    sig2 = dev.Take(200);
                    sig = Enumerable.Range(0, 2).Select(i => Signal.Concat(sig[i], sig2[i])).ToArray();
                }

                if (k == 0)
                {
                    WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("hatosynthdevice.wav"), sig.Select(x => x.ToArray()).ToArray(), 2, 44100, 32);
                }
            }

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            while (!HatoDSPFast.FastMathWrap.Initialized)  // 重要
            {
                System.Threading.Thread.Sleep(100);
            }

            Stopwatch s = new Stopwatch();
            s.Start();

            int cnt = 1000000;  // 片側カウント
            float[] arr;

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)Math.Pow(2.0, i * 96.0 / cnt)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("math_pow.wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Pow2(i * 96.0 / cnt)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_pow2.wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Saw(i * 4 * Math.PI / cnt, 9)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_saw(n,9).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Saw(i * 4 * Math.PI / cnt, 4)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_saw(n,4).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Saw(i * 4 * Math.PI / cnt, 1)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_saw(n,1).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Tri(i * 4 * Math.PI / cnt, 9)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_tri(n,9).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Tri(i * 4 * Math.PI / cnt, 4)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_tri(n,4).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Tri(i * 4 * Math.PI / cnt, 1)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_tri(n,1).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Impulse(i * 4 * Math.PI / cnt, 9)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_imp(n,9).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Impulse(i * 4 * Math.PI / cnt, 4)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_imp(n,4).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Impulse(i * 4 * Math.PI / cnt, 1)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_imp(n,1).wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)Math.Sin(i * 4 * Math.PI / cnt)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("math_sin.wav"), new[] { arr }, 1, 44100, 32);

            arr = Enumerable.Range(-cnt, 2 * cnt).Select(i => (float)HatoDSPFast.FastMathWrap.Sin(i * 4 * Math.PI / cnt)).ToArray();
            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("fastmath_sin.wav"), new[] { arr }, 1, 44100, 32);

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            while (!HatoDSPFast.FastMathWrap.Initialized)
            {
                System.Threading.Thread.Sleep(100);
            }

            Stopwatch s = new Stopwatch();
            s.Start();

            string json = System.IO.File.ReadAllText(HatoPath.FromAppDir("patch.txt"));

            HatoSynthDevice dev = new HatoSynthDevice(json);
            dev.Polyphony = 1;
            dev.ReleasePolyphony = 0;

            int N = 10;  // この値を変更して、実行時間を調整してね！！

            for (int k = 0; k < N; k++)
            {
                Signal[] sig2 = null;

                dev.NoteOn(63 + 9);

                for (int j = 0; j < 4000; j++)
                {
                    sig2 = dev.Take(200);
                }
            }

            s.Stop();
            label1.Text = "" + s.ElapsedMilliseconds + "ms,\r\n"
                + "1% == " + 0.01 * (CPU_CLK * (s.ElapsedMilliseconds * 0.001)) / (200 * 100 * N) + " clk/smp,\r\n"  // 注：ステレオなら1/2倍だし、Rainbowの子になっていれば1/7倍となる
                + "calc_rate == " + (200 * 100 * N / 44100.0) / (s.ElapsedMilliseconds * 0.001) + "倍";
            // 注：シングルスレッドで計算をしているという前提
        }
    }
}
