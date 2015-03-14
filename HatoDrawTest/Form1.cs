using HatoLib;
using HatoBMSLib;
using HatoDraw;
using HatoSound;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoDrawTest
{
    public partial class Form1 : Form
    {
        // VS2012のC#プロジェクトのプラットフォーム構成(Any CPU・32ビットの優先)
        // http://sourcechord.hatenablog.com/entry/20130412/1365789867
        //

        public Form1()
        {
            InitializeComponent();
        }

        HatoDrawDevice hdraw;

        private void button1_Click(object sender, EventArgs e)
        {
            hdraw = new HatoDrawDevice()
            {
                ClientWidth = 640,
                ClientHeight = 480,
                DPI = 96,
            };

            BitmapData bmp = null;

            Form form2 = hdraw.OpenForm();

            form2.KeyDown += (o, ev) =>
            {
                if (ev.KeyCode == Keys.Z)
                {
                    PlayFile(form2, @"b_drums1c_v100l16o3c.wav");
                }
                if (ev.KeyCode == Keys.X)
                {
                    PlayFile(form2, @"b_drums1c_v96l16o3cp.wav");
                }
                if (ev.KeyCode == Keys.C)
                {
                    PlayFile(form2, @"b_drums1c_v100l16o3g.wav");
                }
                if (ev.KeyCode == Keys.V)
                {
                    PlayFile(form2, @"b_drums1c_v100l16o3gp.wav");
                }
            };


            hdraw.Start(
                (rt) =>
            {
                //bmp = new BitmapData(rt, @"1.jpg");
            },
                (rt) =>
            {
                rt.ClearWhite();

                //rt.DrawBitmap(bmp, 100f, 100f);

                using (var brush = new ColorBrush(rt, 0xFF0000))
                {
                    //rt.FillRectangle(5f, 5f, 630f, 470f, brush);
                    rt.FillRectangle(10f, 10f, 20f + DateTime.Now.Millisecond / 2, 20f, brush);
                }
            });
        }

        HatoSoundDevice hsound;

        private void button2_Click(object sender, EventArgs e)
        {
            if (hsound == null)
            {
                hsound = new HatoSoundDevice(this);  // thisでもいいのか？
            }

            SecondaryBuffer sbuf = new SecondaryBuffer(hsound, @"1.wav");
            sbuf.Play();

        }

        private void PlayFile(Form form, string filename)
        {
            // スレッド問題
            // HatoDrawとHatoSoundは統合したほうが良い感じもあるというアレ

            SecondaryBuffer sbuf = new SecondaryBuffer(new HatoSoundDevice(form), filename);
            sbuf.Play();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (hsound == null)
            {
                hsound = new HatoSoundDevice(this);  // thisでもいいのか？
            }

            int j = 0;
            SecondaryBuffer sbuf = new SecondaryBuffer(hsound, 32768, 1);
            sbuf.PlayLoop((fbuf, count) =>
            {
                Console.WriteLine("j = " + j);
                for (int i = 0; i < count; i++)
                {
                    fbuf[0][i] = (float)(0.1 * Math.Sin(((-Math.Exp(-j * 0.00007) * 5000 + j) * 1 + 1) * 0.05) * (Math.Exp(-j * 0.003) + 0.1));
                    j++;
                }
            });
        }

        short[] ObjectPosX = {
            0,60,100,140,180,220,360,0,260,300,
	        0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0,
            0,360,400,440,480,520,640,0,560,600,
	        0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0,
        };
        short[] ObjectColor = {
            0,4,1,4,1,4,0,0,1,4,
	        0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0,
            0,4,1,4,1,4,0,0,1,4,
	        0,0,0,0,0,0,0,0,0,0,
        	0,0,0,0,0,0,0,0,0,0,
	        0,0,0,0,0,0
        };

        string ConsoleMessage = "";

        private void TraceMessage(string text)
        {
            ConsoleMessage = text + "\n" + ConsoleMessage;  // StringBuilder使えない
            Console.WriteLine(text);
        }
        private void TraceWarning(string text)
        {
            ConsoleMessage = text + "\n" + ConsoleMessage;  // StringBuilder使えない
            Console.WriteLine(text);
        }
        
        private async void button4_Click(object sender, EventArgs e)
        {
            bool autoplay = checkBox_autoplay.Checked;

            Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            thisProcess.PriorityClass = ProcessPriorityClass.High;

            hdraw = new HatoDrawDevice()
            {
                DeviceIndependentWidth = 640,
                DeviceIndependentHeight = 480,
                ClientWidth = 640,
                ClientHeight = 480,
                DPI = 96,
                SyncInterval = 1,
            };

            BitmapData bmp = null;
            BitmapData bomb = null;
            BitmapData font = null;

            Dictionary<int, SecondaryBuffer> keysound = new Dictionary<int, SecondaryBuffer>();
            Dictionary<int, double> lastkeydowntime = new Dictionary<int, double>();

            Stopwatch s = new Stopwatch();

            Form form2 = hdraw.OpenForm();

            form2.KeyDown += (o, ev) =>
            {
                SecondaryBuffer buf;
                if (ev.KeyCode == Keys.Z) { lastkeydowntime[36 + 1] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 1, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.S) { lastkeydowntime[36 + 2] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 2, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.X) { lastkeydowntime[36 + 3] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 3, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.D) { lastkeydowntime[36 + 4] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 4, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.C) { lastkeydowntime[36 + 5] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 5, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.F) { lastkeydowntime[36 + 8] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 8, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.V) { lastkeydowntime[36 + 9] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 9, out buf)) { buf.StopAndPlay(0); } }
                if (ev.KeyCode == Keys.B) { lastkeydowntime[36 + 6] = s.ElapsedMilliseconds / 1000.0; if (keysound.TryGetValue(36 + 6, out buf)) { buf.StopAndPlay(0); } }
            };

            if (hsound == null)
            {
                hsound = new HatoSoundDevice(this);  // thisでもいいのか？
            }
            var b = new BMSStruct(new FileStream(@"1.bml", FileMode.Open, FileAccess.Read));

            b.WavDefinitionList[2] = b.WavDefinitionList[1];

            b.ToString();


            var dict = new Dictionary<int, SecondaryBuffer>();

            //Console.WriteLine("Wav Load Completed");

            s.Start();

            double elapsedsec = 0;
            double starttime = 0;
            double SecPerSec = 1.0;
            double HiSpeed = 0.6;

            double WavFileLoadingDelayTime = 1.0;

            {
                int left = 0;
                int right = 0;
                hdraw.Start(
                    (rt) =>
                    {
                        bmp = new BitmapData(rt, @"1.jpg");
                        bomb = new BitmapData(rt, @"2.png");
                        font = new BitmapData(rt, @"3.png");
                        
                        //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                    },
                    (rt) =>
                    {
                        double JustDisplacementSeconds = s.ElapsedMilliseconds / 1000.0 + starttime - WavFileLoadingDelayTime;
                        double JustDisplacementTime = b.transp.SecondsToBeat(JustDisplacementSeconds);
                        double AppearDisplacementTime = JustDisplacementTime + 4.0;

                        rt.ClearBlack();

                        rt.DrawText(font, ConsoleMessage, 0, 0);

                        //rt.DrawBitmap(bmp, 100f, 100f, 0.5f);

                        for (; left < b.SoundBMObjects.Count; left++)  // 消える箇所、left <= right
                        {
                            var x = b.SoundBMObjects[left];
                            if (x.Beat >= JustDisplacementTime) break;
                        }
                        for (; right < b.SoundBMObjects.Count; right++)  // 出現する箇所、left <= right
                        {
                            var x = b.SoundBMObjects[right];
                            if (x.Beat >= AppearDisplacementTime) break;
                        }

                        ColorBrush blackpen = new ColorBrush(rt, 0x000000);

                        Dictionary<int, ColorBrush> brushes = new Dictionary<int, ColorBrush>();
                        brushes[4] = new ColorBrush(rt, 0xCCCCCC);
                        brushes[3] = new ColorBrush(rt, 0xCCCC00);
                        brushes[2] = new ColorBrush(rt, 0x008800);
                        brushes[1] = new ColorBrush(rt, 0x0033CC);
                        brushes[0] = new ColorBrush(rt, 0xCC0000);

                        //Console.WriteLine(left + " / " + right);
                        for (int i = left; i < right; i++)
                        {
                            var x = b.SoundBMObjects[i];
                            if (x.Beat >= starttime)
                            {
                                var displacement = (JustDisplacementTime - x.Beat) * HiSpeed;  // <= 0
                                if (x.IsPlayable())
                                {
                                    //rt.DrawRectangle(30f + ObjectPosX[(x.BMSChannel-36) % 72], 400f + (float)displacement * 500f, 32f, 12f, blackpen, 3.0f);
                                    //rt.FillRectangle(30f + ObjectPosX[(x.BMSChannel - 36) % 72], 400f + (float)displacement * 500f, 32f, 12f, brushes[ObjectColor[(x.BMSChannel - 36) % 72]]);
                                }
                            }
                        }
                        if (autoplay)
                        {
                            for (int i = left - 20; i < right; i++)
                            {
                                if (i < 0) continue;

                                var x = b.SoundBMObjects[i];
                                if (x.Beat >= starttime)
                                {
                                    if (x.IsPlayable())
                                    {
                                        var displacement = (JustDisplacementSeconds - x.Seconds) * 1.2;  // >= 0
                                        int idx = (int)Math.Floor(displacement * 30) + 1;

                                        if (idx <= 0)
                                        {
                                            //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                                            rt.DrawBitmapSrc(bomb,
                                                30f + ObjectPosX[(x.BMSChannel - 36) % 72] * 1.5f - 32f + 16f + (float)displacement * 25f, -((float)x.Measure + 1) % 0.5f * 2f * 360 + 420f - 32f,
                                                0, 0,
                                                64, 64,
                                                (float)Math.Exp(+3 * displacement) * 1.0f, 1.0f);
                                            rt.DrawBitmapSrc(bomb,
                                                30f + ObjectPosX[(x.BMSChannel - 36) % 72] * 1.5f - 32f + 16f - (float)displacement * 25f, -((float)x.Measure + 1) % 0.5f * 2f * 360 + 420f - 32f,
                                                0, 0,
                                                64, 64,
                                                (float)Math.Exp(+3 * displacement) * 1.0f, 1.0f);
                                        }
                                        else if (idx < 32)
                                        {
                                            //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                                            rt.DrawBitmapSrc(bomb,
                                                30f + ObjectPosX[(x.BMSChannel - 36) % 72] * 1.5f - 32f + 16f, -((float)x.Measure + 1) % 0.5f * 2f * 360 + 420f - 32f,
                                                idx % 8 * 64, idx / 8 * 64,
                                                64, 64,
                                                1.0f, 1.0f);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var kvpair in lastkeydowntime)
                            {
                                var displacement = s.ElapsedMilliseconds / 1000.0 - kvpair.Value;  // >= 0
                                rt.DrawBitmap(bomb, 30f + ObjectPosX[(kvpair.Key - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                            }
                        }
                        //rt.FillRectangle(5f, 5f, 630f, 470f, brush);
                        //rt.FillRectangle(10f, 10f, 20f + DateTime.Now.Millisecond / 2, 20f, brush);

                        blackpen.Dispose();
                        brushes[0].Dispose();
                        brushes[1].Dispose();
                        brushes[2].Dispose();
                        brushes[3].Dispose();
                        brushes[4].Dispose();
                    });
            }

            await Task.Run(() => Parallel.Invoke(
                async () =>  // wavの読み込み
                {
                    foreach (var x in b.SoundBMObjects)
                    {
                        while (x.Seconds * SecPerSec >= elapsedsec + starttime)
                        {
                            //Thread.Sleep(100);
                            await Task.Delay(100);
                            elapsedsec = s.ElapsedMilliseconds / 1000.0;
                        }

                        if (x.Seconds * SecPerSec >= starttime)
                        {
                            SecondaryBuffer sbuf;

                            if (!dict.TryGetValue(x.Wavid, out sbuf))
                            {
                                await Task.Run(() =>
                                {
                                    string wavfilename;
                                    if (b.WavDefinitionList.TryGetValue(x.Wavid, out wavfilename))
                                    {
                                        if (File.Exists(@"\" + wavfilename))
                                        {
                                            sbuf = new SecondaryBuffer(hsound, @"\" + b.WavDefinitionList[x.Wavid]);
                                            
                                            lock (dict)
                                            {
                                                dict[x.Wavid] = sbuf;
                                                TraceMessage("    " + b.WavDefinitionList[x.Wavid] + " Load Completed (" + dict.Count + "/" + b.WavDefinitionList.Count + ")");
                                            }
                                        }
                                        else
                                        {
                                            TraceWarning("  Warning: " + b.WavDefinitionList[x.Wavid] + " does NOT Exist!!");
                                        }
                                    }
                                    else
                                    {
                                        TraceWarning("  Warning: #WAV" + BMConvert.ToBase36(x.Wavid) + " is NOT Defined!!");
                                    }
                                });
                            }
                        }
                    }
                },
                async () =>  // キー音の割り当て
                {
                    foreach (var x in b.SoundBMObjects)
                    {
                        while (x.Seconds * SecPerSec >= elapsedsec + starttime - WavFileLoadingDelayTime + 0.3)
                        {
                            //Thread.Sleep(100);
                            await Task.Delay(50);
                            elapsedsec = s.ElapsedMilliseconds / 1000.0;
                        }

                        if (x.Seconds * SecPerSec >= starttime)
                        {
                            SecondaryBuffer sbuf;

                            if (dict.TryGetValue(x.Wavid, out sbuf))
                            {
                                if (x.IsPlayable())  // autoplayかどうかによらない
                                {
                                    lock (keysound)
                                    {
                                        keysound[(x.BMSChannel - 36) % 72 + 36] = sbuf;
                                    }
                                }
                            }
                            else
                            {
                                TraceWarning("  Warning : \"" + b.WavDefinitionList.GetValueOrDefault(x.Wavid) + "\" (key sound) is not loaded yet...");
                            }
                        }

                    }
                },
                async () =>  // wavの再生
                {
                    foreach (var x in b.SoundBMObjects)
                    {
                        while (x.Seconds * SecPerSec >= elapsedsec + starttime - WavFileLoadingDelayTime)
                        {
                            // TaskSchedulerException・・・？？
                            // Thread.Sleep(10);
                            await Task.Delay(5);
                            elapsedsec = s.ElapsedMilliseconds / 1000.0;
                        }

                        if (x.Seconds * SecPerSec >= starttime)
                        {
                            SecondaryBuffer sbuf;

                            if (autoplay || !x.IsPlayable())
                            {
                                if (dict.TryGetValue(x.Wavid, out sbuf))
                                {
                                    sbuf.StopAndPlay(autoplay ? 0.0 : -4.0);
                                }
                                else
                                {
                                    TraceWarning("  Warning : " + b.WavDefinitionList.GetValueOrDefault(x.Wavid) + " is not loaded yet...");
                                }
                            }
                        }
                    }
                }));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            /*
            for (int i = 0; i < 100; i++)
            {
                int i2 = i;
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    Console.WriteLine("finished!" + i2);
                });
            }*/

            Parallel.For(0, 100, (int i) =>
            {
                Thread.Sleep(30);
                //Console.WriteLine("finished!" + i);
            });
            Console.WriteLine("finished!");
            // ↑ 時間が掛かる

            Parallel.For(0, 100, async (int i) =>
            {
                await Task.Delay(30);
            });
            Console.WriteLine("finished!");
            // ↑ 1秒で終了する

            Parallel.For(0, 100, (int i) => Thread.Sleep(30));
            Parallel.For(0, 100, async (int i) => await Task.Delay(1000));
        }
    }
}
