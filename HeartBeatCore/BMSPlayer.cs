using HatoBMSLib;
using HatoLib;
using HatoDraw;
using HatoSound;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HatoPlayer;

namespace HeartBeatCore
{
    public class BMSPlayer
    {
        public BMSStruct b;  // ガベージコレクタに回収されてはならないんだぞ

        // TODO: これ↓の初期化処理
        public GameRegulation regulation = new HeartBeatRegulation();

        public bool Playside2P = false;

        public bool autoplay = false;

        public bool BMSMode = false;  // TODO: 分岐はハードコーディングじゃなくてスキンで解決したい

        public float RingShowingPeriodByMeasure = 2.0f;

        HatoDrawDevice hdraw;
        HatoPlayerDevice hplayer;

        short[] ObjectPosX = {
            0,60,100,140,180,220,0,0,260,300,
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

        string ConsoleMessage = "Waiting...\n";

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

        public BMSPlayer()
        {
        }

        Form form;

        public Form OpenForm()
        {
            hdraw = new HatoDrawDevice()
            {
                DeviceIndependentWidth = 853,
                DeviceIndependentHeight = 480,
                ClientWidth = 853,
                ClientHeight = 480,
                DPI = 96,
                SyncInterval = 1,
            };

            // 本当に非同期である必要があるのか？
            return form = hdraw.OpenForm();
        }

        public void Run()
        {
            hdraw.Start(
               (rt) =>
               {
                   string pathfont1 = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "font1.png");
                   font = new BitmapData(rt, pathfont1);
               },
               (rt) =>
               {
                   rt.ClearBlack();

                   rt.DrawText(font, ConsoleMessage, 4, 4);

                   if (onPaint != null)
                   {
                       onPaint(rt);
                   }
               });
        }

        BitmapData bmp = null;
        BitmapData bomb = null;
        BitmapData bar = null;
        BitmapData bar_white = null;
        BitmapData judgement = null;
        BitmapData font = null;
        Action<RenderTarget> onPaint;

        BitmapData bga_front = null;
        BitmapData bga_back = null;
        BitmapData bga_poor = null;

        Stopwatch s = new Stopwatch();

        double WavFileLoadingDelayTime = 30.0;  // 先読みする時間量
        double DelayingTimeBeforePlay = 1.0;  // 読み込み完了から曲が再生されるまでの時間
        double PlayFrom = 0.0;  // 曲の再生を開始する地点（秒）

        double CurrentSongPosition()
        {
            return s.ElapsedMilliseconds / 1000.0 + PlayFrom - DelayingTimeBeforePlay;
        }

        public async void LoadAndPlay(string path, int startmeasure = 0)
        {
            Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

            Dictionary<int, int> keysound = new Dictionary<int, int>();
            //Dictionary<int, double> lastkeydowntime = new Dictionary<int, double>();

            s = new Stopwatch();

            Dictionary<int, double> lastKeyDown = new Dictionary<int, double>();
            Queue<KeyEvent> keyEventList = new Queue<KeyEvent>();
            KeyEvent lastKeyEvent = null;

            hdraw.OnKeyDown = (o, ev, ddrawForm) =>
            {
                // lastkeydowntime[36 + 2] = CurrentSongPosition(); 
                // if (keysound.TryGetValue(36 + 2, out wavid)) {
                //    hplayer.PlaySound(wavid, true);
                // }

                int? keyid = null;

                if (ev.KeyCode == Keys.Z) { keyid = 1; }
                if (ev.KeyCode == Keys.S) { keyid = 2; }
                if (ev.KeyCode == Keys.X) { keyid = 3; }
                if (ev.KeyCode == Keys.D) { keyid = 4; }
                if (ev.KeyCode == Keys.C) { keyid = 5; }
                if (ev.KeyCode == Keys.F) { keyid = 8; }
                if (ev.KeyCode == Keys.V) { keyid = 9; }
                if (!Playside2P)
                {
                    if (ev.KeyCode == Keys.ShiftKey) { keyid = 6; }
                }
                else
                {
                    if (ev.KeyCode == Keys.B) { keyid = 6; }
                }

                if (keyid != null)
                {
                    int wavid;
                    double CSP = CurrentSongPosition();

                    lastKeyEvent = new KeyEvent
                    {
                        keyid = (int)keyid,
                        seconds = CSP
                    };
                    lastKeyDown[(int)keyid] = CSP;
                    
                    keyEventList.Enqueue(lastKeyEvent);

                    if (keysound.TryGetValue(36 + (int)keyid, out wavid))
                    {
                        hplayer.PlaySound(wavid, true);
                    }
                }
            };

            b = new BMSStruct(new FileStream(path, FileMode.Open, FileAccess.Read));
            b.DirectoryName = Path.GetDirectoryName(path);

            int maxscore = b.SoundBMObjects.Where(x => x.IsPlayable()).Count();
            var scoretable = new Dictionary<int, int>();  // not exist:poor, 0:bad, 1:good, 2:great, 3:pg

            {
                string str;

                str = b.Subartist; if (str != null) TraceMessage("    " + str);
                str = b.Artist; if (str != null) TraceMessage(str);
                str = b.Subtitle; if (str != null) TraceMessage("    " + str);
                str = b.Title; if (str != null) TraceMessage(str);
                str = b.Genre; if (str != null) TraceMessage("GENRE: " + str);
                TraceMessage("INIT BPM: " + b.BPM);
                TraceMessage("2/3 BPM: " + b.CalcTempoMedian(0.67));
                TraceMessage("PLAYLEVEL: " + b.Playlevel);
                TraceMessage("DIFFICULTY: " + b.Difficulty);
                TraceMessage("NOTES COUNT: " + b.AllBMObjects.Where(x => x.IsSound() && x.IsPlayable()).Count());
                TraceMessage("TOTAL: " + b.Total);
            }

            PlayFrom = b.transp.BeatToSeconds(b.transp.MeasureToBeat(new Rational(startmeasure)));
            {
                var b1 = b.SoundBMObjects.FirstOrDefault();
                if (b1 != null) PlayFrom = Math.Max(b1.Seconds, PlayFrom);
                // あんまり変なことすると例えばBGAがあった時とかどうするのよ
            }

            var dictbmp = new Dictionary<int, BitmapData>();

            TraceMessage("Timer Started.");

            double tempomedian = b.CalcTempoMedian(0.667);  // 3分の2くらいが低速だったらそっちに合わせようかな、という気持ち
            double HiSpeed = 0.6 * (150.0 / tempomedian);

            int PreLoadingTimeoutMilliSeconds = 20000;

            DelayingTimeBeforePlay = autoplay ? 1.0 :
                (b.SoundBMObjects.Where(x => x.IsPlayable()).Count() >= 1 ?
                Math.Max(1.0, 3.0 - (b.SoundBMObjects.Where(x => x.IsPlayable()).First().Seconds - PlayFrom)) : 1.0);
            // (1本wavかつwav形式だと1秒では読み込めないことがあるかも)
            // というか一本wav(Delicious Rabbitとか)問題はいろいろと解決しなければならなさそう
            // 8192サンプル間隔くらいごとにストリーミングした方がいいのでは
            // まあでもそれより先にogg対応な
            // と思ったら、間違ったところでawaitしていただけだった・・・

            if (hplayer == null)
            {
                hplayer = new HatoPlayerDevice(form, b);  // thisでもいいのか？
            }

            //s.Start();
            Stopwatch loadingTime = new Stopwatch();
            loadingTime.Start();

            #region プリローディング
            {
                // ああ、StartNewすればいいのか・・・
                Task task = Task.Factory.StartNew( () =>
                {
                    Parallel.ForEach(b.SoundBMObjects, (sb) =>
                    {
                        if (sb.Seconds < PlayFrom || PlayFrom + WavFileLoadingDelayTime < sb.Seconds) return;  // 等号が入るかどうかに注意な！

                        hplayer.PrepareSound(sb.Wavid);

                        // TraceMessage("    Preload " + sb.Wavid);
                        // 一部の音しかプリロードされないことがある・・・？
                        // で、どういう時かというと、NVorbisがぽしゃった時っぽい。
                        // やっぱりNVorbisはやめよう
                    });
                });

                // Asynchronously wait for Task<T> to complete with timeout
                // http://stackoverflow.com/questions/4238345/asynchronously-wait-for-taskt-to-complete-with-timeout
                if (await Task.WhenAny(task, Task.Delay(PreLoadingTimeoutMilliSeconds)) == task)
                {
                    TraceMessage("(^^) Load OK!");
                    // task completed within timeout
                }
                else
                {
                    TraceMessage("Time out...");
                    // timeout logic
                }
            }
            #endregion

            loadingTime.Stop();

            TraceMessage("    Loading Time: " + loadingTime.ElapsedMilliseconds + "ms");

            var silence = hplayer.LoadAudioFileOrGoEasy(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "silence20s.wav"));
            silence.StopAndPlay();  // 無音を再生させて、プライマリバッファが稼働していることを保証させる

            s.Start();

            #region 描画処理（長い）
            {
                int left = 0;
                int right = 0;
                int hitzoneLeft = 0;
                int hitzoneRight = 0;
                //form = hdraw.OpenForm();
                {
                    var rt = hdraw.HatoRenderTarget;
                    if (b.Stagefile != null && File.Exists(b.ToFullPath(b.Stagefile)))
                    {
                        bmp = new BitmapData(rt, b.ToFullPath(b.Stagefile));
                    }
                    //bomb = new BitmapData(rt, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "bomb1.png"));
                    bomb = new BitmapData(rt, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ring1.png"));
                    bar = new BitmapData(rt, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "bar1.png"));
                    bar_white = new BitmapData(rt, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "bar1_white.png"));

                    judgement = new BitmapData(rt, Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "judgement.png"));
                    //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.AboveNormal;

                }
                onPaint = (rt) =>
                   {
                       BMTime current = new BMTime
                       {
                           Disp = b.transp.SecondsToBeat(CurrentSongPosition()),
                           Beat = b.transp.SecondsToBeat(CurrentSongPosition()),
                           Seconds = CurrentSongPosition(),
                           Measure = 0
                       };

                       PlayingState ps = new PlayingState
                       {
                           Combo = 0,
                           Gauge = 0,
                           CurrentMaximumExScore = 0,
                           TotalExScore = 0,
                           LastJudgement = Judgement.None,
                           Current = current
                       };

                       //double JustDisplacement = b.transp.SecondsToBeat(CurrentSongPosition());
                       double AppearDisplacement = current.Disp + 4.0;

                       #region BGA表示
                       if (bmp != null)
                       {
                           rt.DrawBitmap(bmp, 0f, 0f, 0.10f, 480f / bmp.Height);
                       }
                       if (bga_back != null)
                       {
                           rt.DrawBitmap(bga_back, 853f - 256f - 10f, 10f, 1.0f, 256f / bga_back.Height);
                       }
                       if (bga_front != null)
                       {
                           rt.DrawBitmap(bga_front, 853f - 256f - 10f, 10f, 1.0f, 256f / bga_front.Height);
                       }
                       #endregion

                       #region ゲージ・スコアレート表示
                       {
                           int scoretotal = scoretable.Select(x => x.Value >= 1 ? 1 : 0).Sum();
                           float scorerate = scoretotal / (float)(maxscore * 1);
                           float scorepixel = (480 - 276 - 20) * scoretotal / (float)(maxscore * 1);

                           rt.FillRectangle(780f - 40f, 276f, 30f, 480 - 276 - 20, new ColorBrush(rt, 0x666666));
                           rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, new ColorBrush(rt, 0xFF8888));

                           rt.DrawText(font, "Gauge:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 300, 1.0f);
                       }

                       {
                           int scoretotal = scoretable.Select(x => x.Value >= 2 ? x.Value - 1 : 0).Sum();
                           float scorerate = scoretotal / (float)(maxscore * 2);
                           float scorepixel = (480 - 276 - 20) * scoretotal / (float)(maxscore * 2);

                           rt.FillRectangle(780f, 276f, 30f, 480 - 276 - 20, new ColorBrush(rt, 0x666666));
                           rt.FillRectangle(780f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, new ColorBrush(rt, 0x88FF88));

                           rt.DrawText(font, "Rate:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 400f, 1.0f);
                       }
                       #endregion

                       for (; left < b.SoundBMObjects.Count; left++)  // 消える箇所、left <= right
                       {
                           var x = b.SoundBMObjects[left];
                           if (x.Beat >= current.Disp) break;
                       }
                       for (; right < b.SoundBMObjects.Count; right++)  // 出現する箇所、left <= right
                       {
                           var x = b.SoundBMObjects[right];
                           if (x.Beat >= AppearDisplacement) break;
                       }
                       for (; hitzoneLeft < b.SoundBMObjects.Count; hitzoneLeft++)  // 消える箇所、left <= right
                       {
                           var x = b.SoundBMObjects[hitzoneLeft];
                           if (x.Seconds + regulation.JudgementWindowSize >= current.Seconds) break;
                       }
                       for (; hitzoneRight < b.SoundBMObjects.Count; hitzoneRight++)  // 出現する箇所、left <= right
                       {
                           var x = b.SoundBMObjects[hitzoneRight];
                           if (x.Seconds - regulation.JudgementWindowSize >= current.Seconds) break;
                       }

                       #region キー入力キューの消化試合
                       // キー入力に最近のオブジェを探す
                       // 当たり判定があった場合はいろいろする
                       while(keyEventList.Count!=0){
                           var kvpair = keyEventList.Dequeue();
                           double min = double.MaxValue;
                           BMObject minAt = null;

                           for (int i = hitzoneLeft; i < hitzoneRight; i++)
                           {
                               var obj = b.SoundBMObjects[i];
                               var dif = Math.Abs(obj.Seconds - CurrentSongPosition());
                               if (dif < min &&
                                   obj.Broken == false &&
                                   (obj.BMSChannel - 36) % 72 == kvpair.keyid)
                               {
                                   min = dif;
                                   minAt = b.SoundBMObjects[i];
                               }
                           }

                           if (minAt != null)
                           {
                               Judgement judge = regulation.SecondsToJudgement(min);
                               if (judge != Judgement.None)  // 判定無しでなければ
                               {
                                   minAt.Broken = true;
                                   minAt.Judge = regulation.SecondsToJudgement(min);
                                   minAt.BrokeAt = kvpair.seconds;
                               }
                           }
                       }
                       #endregion

                       ColorBrush blackpen = new ColorBrush(rt, 0x000000);

                       Dictionary<int, ColorBrush> brushes = new Dictionary<int, ColorBrush>();
                       brushes[4] = new ColorBrush(rt, 0xCCCCCC);
                       brushes[3] = new ColorBrush(rt, 0xCCCC00);
                       brushes[2] = new ColorBrush(rt, 0x008800);
                       brushes[1] = new ColorBrush(rt, 0x0066FF);
                       brushes[0] = new ColorBrush(rt, 0xFF3333);


                       float ttt = 1.5f; // デフォルトで1.0

                       //Console.WriteLine(left + " / " + right);
                       if (false)
                       {
                           for (int i = left; i < right; i++)
                           {
                               var x = b.SoundBMObjects[i];
                               if (x.Beat >= PlayFrom)
                               {
                                   var displacement = (current.Disp - x.Beat) * HiSpeed;  // <= 0
                                   if (x.IsPlayable() && x.Seconds >= PlayFrom && (x.BMSChannel / 36 <= 2 || 5 <= x.BMSChannel / 36))
                                   {
                                       var xpos = 40f + ObjectPosX[(x.BMSChannel + (Playside2P ? 0 : 1) * 36) % 72] * 0.8f;
                                       rt.DrawRectangle(xpos, 400f + (float)displacement * 500f, 32f, 12f, blackpen, 3.0f);
                                       rt.FillRectangle(xpos, 400f + (float)displacement * 500f, 32f, 12f, brushes[ObjectColor[(x.BMSChannel - 36) % 72]]);
                                   }
                               }
                           }
                       }

                       {
                           {
                               #region Ringモード 非オートプレイの場合（キーフラッシュ）
                               foreach (var bmschannel in new int[] { 36 + 1, 36 + 3, 36 + 5, 36 + 9 })
                               {
                                   var xpos = 40f + ObjectPosX[(bmschannel + (Playside2P ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;

                                   rt.FillRectangle(
                                                   xpos - 32f + 16f + 96f - 16f, 0,
                                                   48, 480,
                                                   new ColorBrush(rt, 0x888888, 0.12f));
                               }
                               #endregion

                               #region キー入力時間を示す白いバー（多分）
                               while (lastKeyEvent != null)
                               {
                                   var xpos = 40f + ObjectPosX[(lastKeyEvent.keyid + 36 + (Playside2P ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;
                                   var xpos2 = 40f + 180 * ttt * 0.8f;
                                   var displacement = CurrentSongPosition() - lastKeyEvent.seconds;  // >= 0

                                   rt.FillRectangle(
                                                   xpos - 32f + 16f + 96f - 16f, 0,
                                                   48, 480,
                                                   new ColorBrush(rt, 0x00AAFF, (float)Math.Exp(-6 * displacement) * 0.20f));

                                   if (((int)(displacement * 30)) < 16)
                                   {
                                       rt.DrawBitmapSrc(bar_white,
                                           xpos2 - 256f + 16f, -(((float)b.transp.SecondsToBeat(lastKeyEvent.seconds) / 4 + 0.3f) % RingShowingPeriodByMeasure - 0.3f) / RingShowingPeriodByMeasure * 360 + 420f - 8f,
                                           //0, 0 + 12, 
                                           0, ((int)(displacement * 30)) * 16,
                                           512, 16,
                                           0.5f);
                                   }
                                   if (((int)(displacement * 30)) < 16)
                                   {
                                       rt.DrawBitmapSrc(bar_white,
                                           xpos2 - 256f + 16f, -(((float)b.transp.SecondsToBeat(lastKeyEvent.seconds) / 4 + 0.3f) % RingShowingPeriodByMeasure - 0.3f + RingShowingPeriodByMeasure) / RingShowingPeriodByMeasure * 360 + 420f - 8f,
                                           //0, 0 + 12,
                                           0, ((int)(displacement * 30)) * 16,
                                           512, 16,
                                           0.5f);
                                   }
                                   break;
                               }
                               #endregion

                               #region Ringモード 非オートプレイの場合
                               for (int i = left - 50; i < right; i++)
                               {
                                   if (i < 0) continue;

                                   var x = b.SoundBMObjects[i];
                                   if (x.Beat >= PlayFrom)
                                   {
                                       if (x.IsPlayable())
                                       {
                                           var posdisp = (CurrentSongPosition() - x.Seconds) * 2.4;
                                           var displacement = (CurrentSongPosition() - x.Seconds) * 1.2 / RingShowingPeriodByMeasure;  // >= 0
                                           //int idx = (int)Math.Floor((b.transp.BeatToSeconds(JustDisplacement) - x.Seconds) * 30) + 1;
                                           int idx = 0;
                                           var xpos = 40f + ObjectPosX[(x.BMSChannel + (Playside2P ? 0 : 1) * 36) % 72] * ttt * 0.8f;
                                           var xpos2 = 40f + 180 * ttt * 0.8f;

                                           if (x.Broken)
                                           {
                                               // キー押し下しがあった
                                               idx = (int)Math.Floor((CurrentSongPosition() - x.BrokeAt) * 60) + 1;
                                           }

                                           if (idx <= 0)
                                           {
                                               // キー押し下しがなかった場合の処理

                                               idx = (int)Math.Floor((CurrentSongPosition() - x.Seconds) * 30) + 1;
                                               var opac = (idx >= 0 ? (0.6f / (idx * 6f + 1f)) : 1.0f);

                                               if (posdisp > 0) posdisp = 0;
                                               if (displacement > 0) displacement = 0;

                                               //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                                               rt.DrawBitmapSrc(bomb,
                                                   xpos - 32f + 16f + (float)posdisp * 25f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 32f,
                                                   0, 0,
                                                   64, 64,
                                                   (float)Math.Exp(+3 * displacement) * 1.0f * opac);
                                               rt.DrawBitmapSrc(bomb,
                                                   xpos - 32f + 16f - (float)posdisp * 25f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 32f,
                                                   0, 0,
                                                   64, 64,
                                                   (float)Math.Exp(+3 * displacement) * 1.0f * opac);
                                               rt.DrawBitmapSrc(bar,
                                                   xpos2 - 256f + 16f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 16f,
                                                   0, 0,
                                                   512, 32,
                                                   (float)Math.Exp(+3 * displacement) * 0.5f * opac);
                                           }
                                           else if (idx < 32)
                                           {
                                               // キー押し下しがあった場合の処理

                                               //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                                               rt.DrawBitmapSrc(bomb,
                                                   xpos - 32f + 16f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 32f,
                                                   idx % 8 * 64, idx / 8 * 64,
                                                   64, 64,
                                                   1.0f);
                                               rt.DrawBitmapSrc(bar,
                                                   xpos2 - 256f + 16f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 16f,
                                                   0, idx / 2 * 32,
                                                   512, 32,
                                                   0.1f);

                                               // keyは、soundbmobjectのインデックス。まあuniqueなら何でもいい
                                               scoretable[i] = (int)x.Judge - 1;  // 0,1,2,3. 3:pg;
                                               if (scoretable[i] < 0) scoretable[i] = 0;

                                               rt.DrawBitmapSrc(judgement,
                                                   xpos - 64f + 16f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 32f + 39f,
                                                   idx / 2 % 2 * 128, (3 - scoretable[i]) * 64,
                                                   128, 64,
                                                   1.0f, 1.0f);

                                           }
                                       }
                                   }
                               }
                               #endregion
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
                   };
            }
            #endregion

            await Task.Run(() => Parallel.Invoke(
                async () =>  // wavの読み込み
                {
                    foreach (var x in b.SoundBMObjects)
                    {
                        while (x.Seconds >= CurrentSongPosition() + WavFileLoadingDelayTime)
                        {
                            await Task.Delay(100);
                        }

                        if (x.Seconds >= PlayFrom)
                        {
                            await Task.Run(() => hplayer.PrepareSound(x.Wavid));
                        }
                    }
                },
                async () =>  // bmpの読み込み
                {
                    foreach (var x in b.GraphicBMObjects)
                    {
                        while (x.Seconds >= CurrentSongPosition() + WavFileLoadingDelayTime)
                        {
                            await Task.Delay(100);
                        }

                        if (x.Seconds >= PlayFrom)
                        {
                            BitmapData sbuf;
                            string fn;

                            if (b.BitmapDefinitionList.TryGetValue(x.Wavid, out fn)
                                && File.Exists(Path.Combine(Path.GetDirectoryName(path), fn))
                                && !dictbmp.TryGetValue(x.Wavid, out sbuf))
                            {
                                // lockの範囲おかしくない？
                                lock (dictbmp)
                                {
                                    dictbmp[x.Wavid] = null;  // 同じ音の多重読み込みを防止
                                }

                                await Task.Run(() =>
                                {
                                    try
                                    {
                                        sbuf = new BitmapData(hdraw.HatoRenderTarget, Path.Combine(Path.GetDirectoryName(path), fn));

                                        lock (dictbmp)
                                        {
                                            dictbmp[x.Wavid] = sbuf;
                                            //TraceMessage("    " + b.WavDefinitionList[x.Wavid] + " Load Completed (" + dict.Count + "/" + b.WavDefinitionList.Count + ")");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        TraceWarning("  Exception: " + e.ToString());
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
                        while (x.Seconds >= CurrentSongPosition() + 0.3)
                        {
                            //Thread.Sleep(100);
                            await Task.Delay(50);
                        }

                        if (x.Seconds >= PlayFrom && x.IsPlayable())  // autoplayかどうかによらない、また、非表示でもOK
                        {
                            keysound[(x.BMSChannel - 36) % 72 + 36] = x.Wavid;

                            //    TraceWarning("  Warning : \"" + b.WavDefinitionList.GetValueOrDefault(x.Wavid) + "\" (key sound) is not loaded yet...");
                        }

                    }
                },
                async () =>  // wavの再生
                {
                    foreach (var x in b.SoundBMObjects)
                    {
                        while (x.Seconds >= CurrentSongPosition())
                        {
                            // TaskSchedulerException・・・？？
                            await Task.Delay(5);
                        }

                        if (x.Seconds >= PlayFrom)
                        {
                            if ((autoplay || !x.IsPlayable()) && !x.IsInvisible())
                            {
                                hplayer.PlaySound(x.Wavid, false);
                            }
                        }
                    }
                },
                async () =>  // bmpの再生
                {
                    foreach (var x in b.GraphicBMObjects)
                    {
                        while (x.Seconds >= CurrentSongPosition())
                        {
                            await Task.Delay(10);
                        }

                        if (x.Seconds >= PlayFrom)
                        {
                            BitmapData bmpdata;

                            if (dictbmp.TryGetValue(x.Wavid, out bmpdata) && bmpdata != null)
                            {
                                    /*
                                     * 04	[objs/bmp] BGA-BASE
                                     * 05
                                     * 06	[objs/bmp] BGA-POOR
                                     * 07	[objs/bmp] BGA-LAYER
                                     */
                                switch (x.BMSChannel)
                                {
                                    case 4:
                                        bga_back = bmpdata;
                                        break;
                                    case 6:
                                        bga_poor = bmpdata;
                                        break;
                                    case 7:
                                        bga_front = bmpdata;
                                        break;
                                    default:
                                        throw new Exception("それ画像じゃない");
                                }
                            }
                            else
                            {
                                TraceWarning("  Warning : " + b.WavDefinitionList.GetValueOrDefault(x.Wavid) + " is not loaded yet...");
                            }
                        }
                    }
                }));
        }

        public void Stop()
        {
        }
    }
}
