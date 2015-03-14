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
using HatoPainter;

namespace HeartBeatCore
{
    public class BMSPlayer
    {
        public BMSStruct b;  // ガベージコレクタに回収されてはならないんだぞ

        // TODO: これ↓の初期化処理
        public GameRegulation regulation = new HeartBeatRegulation();

        // TODO: これ↓の初期化処理
        public Skin skin = new SimpleRingSkin();

        PlayingState ps = new PlayingState();

        public bool Playside2P = false;

        public bool autoplay = false;

        public bool BMSMode = false;  // TODO: 分岐はハードコーディングじゃなくてスキンで解決したい

        // TODO: Skinへの移行
        public float RingShowingPeriodByMeasure
        {
            get
            {
                return skin.RingShowingPeriodByMeasure;
            }
            set
            {
                skin.RingShowingPeriodByMeasure = value;
            }
        }

        HatoDrawDevice hdraw;
        HatoPlayerDevice hplayer;

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
                   string pathfont1 = HatoPath.FromAppDir("font1.png");
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

        BitmapData font = null;
        Action<RenderTarget> onPaint;

        BitmapData bga_front = null;
        BitmapData bga_back = null;
        BitmapData bga_poor = null;

        Stopwatch s = new Stopwatch();

        double WavFileLoadingDelayTime = 30.0;  // 先読み対象とする時間量
        double DelayingTimeBeforePlay = 1.0;  // 読み込み完了から曲が再生されるまでの時間
        double PlayFrom = 0.0;  // 曲の再生を開始する地点（秒）

        double CurrentSongPosition()
        {
            return s.ElapsedMilliseconds / 1000.0 + PlayFrom - DelayingTimeBeforePlay;
        }

        public async void LoadAndPlay(string path, int startmeasure = 0)
        {
            {
                Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
            }

            Dictionary<int, int> keysound = new Dictionary<int, int>();

            s = new Stopwatch();

            // 各キーidに対応する、最後に押したキーの時刻。キーフラッシュ用。
            Dictionary<int, double> lastKeyEventDict = new Dictionary<int, double>();

            // キー入力キューで、フレームごとに消化される。（描画に直接用いることはない）
            Queue<KeyEvent> keyEventList = new Queue<KeyEvent>();

            hdraw.OnKeyDown = (o, ev, ddrawForm) =>
            {
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

                    ps.LastKeyEvent = new KeyEvent
                    {
                        keyid = (int)keyid,
                        seconds = CSP
                    };
                    lastKeyEventDict[(int)keyid] = CSP;

                    keyEventList.Enqueue(ps.LastKeyEvent);

                    if (keysound.TryGetValue(36 + (int)keyid, out wavid))
                    {
                        hplayer.PlaySound(wavid, true);
                    }
                }
            };

            b = new BMSStruct(new FileStream(path, FileMode.Open, FileAccess.Read));
            b.DirectoryName = Path.GetDirectoryName(path);

            int maxscore = b.PlayableBMObjects.Count();

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
                Task task = Task.Factory.StartNew(() =>
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

            var silence = hplayer.LoadAudioFileOrGoEasy(HatoPath.FromAppDir("silence20s.wav"));
            silence.StopAndPlay();  // 無音を再生させて、プライマリバッファが稼働していることを保証させる

            s.Start();

            #region 描画処理（長い）
            {
                int left = 0;
                int right = 0;
                int hitzoneLeft = 0;
                int hitzoneRight = 0;
                int bombzoneLeft = 0;

                {
                    var rt = hdraw.HatoRenderTarget;

                    skin.Load(rt, b);
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

                       // PlayingStateの設定
                       ps.Current = current;

                       double AppearDisplacement = current.Disp + 4.0;

                       #region BGA表示
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
                           // 受け入れレート
                           float scorerate = ps.TotalAcceptance / (float)(maxscore * 1);
                           float maxrate = ps.CurrentMaximumAcceptance / (float)(maxscore * 1);
                           float scorepixel = (480 - 276 - 20) * scorerate;
                           float maxpixel = (480 - 276 - 20) * maxrate;

                           rt.FillRectangle(780f - 40f, 276f, 30f, 480 - 276 - 20, new ColorBrush(rt, 0x666666));
                           rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - maxpixel, 30f, maxpixel, new ColorBrush(rt, 0x884444));
                           rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, new ColorBrush(rt, 0xFF8888));

                           rt.DrawText(font, "Gauge:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 300, 1.0f);
                       }

                       {
                           // スコアレート
                           float scorerate = ps.TotalExScore / (float)(maxscore * regulation.MaxScorePerObject);
                           float maxrate = ps.CurrentMaximumExScore / (float)(maxscore * regulation.MaxScorePerObject);
                           float scorepixel = (480 - 276 - 20) * scorerate;
                           float maxpixel = (480 - 276 - 20) * maxrate;

                           rt.FillRectangle(780f, 276f, 30f, 480 - 276 - 20, new ColorBrush(rt, 0x666666));
                           rt.FillRectangle(780f, 276f + (480 - 276 - 20) - maxpixel, 30f, maxpixel, new ColorBrush(rt, 0x448844));
                           rt.FillRectangle(780f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, new ColorBrush(rt, 0x88FF88));

                           rt.DrawText(font, "Rate:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 400f, 1.0f);
                       }
                       #endregion

                       #region オブジェ範囲の更新
                       for (; left < b.PlayableBMObjects.Count; left++)  // 視界から消える箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[left];
                           if (x.Beat >= current.Disp) break;
                       }
                       for (; right < b.PlayableBMObjects.Count; right++)  // 視界に出現する箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[right];
                           if (x.Beat >= AppearDisplacement) break;
                       }
                       for (; hitzoneLeft < b.PlayableBMObjects.Count; hitzoneLeft++)  // 判定ゾーンから消える箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[hitzoneLeft];
                           if (x.Seconds + regulation.JudgementWindowSize >= current.Seconds) break;
                           else
                           {
                               // 判定ゾーンから外にオブジェクトが出ます
                               if (x.Broken == false)
                               {
                                   x.Broken = true; // これは消してもいいはず
                                   ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                   ps.CurrentMaximumAcceptance += 1;
                               }
                           }
                       }
                       for (; hitzoneRight < b.PlayableBMObjects.Count; hitzoneRight++)  // 判定ゾーンに突入する箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[hitzoneRight];
                           if (x.Seconds - regulation.JudgementWindowSize >= current.Seconds) break;
                       }
                       for (; bombzoneLeft < b.PlayableBMObjects.Count; bombzoneLeft++)  // ボム・キーフラッシュが消える箇所
                       {
                           var x = b.PlayableBMObjects[bombzoneLeft];
                           if (x.Seconds + (regulation.JudgementWindowSize + skin.BombDuration) >= current.Seconds) break;
                       }
                       #endregion

                       #region キー入力キューの消化試合
                       // キー入力に最近のオブジェを探す
                       // 当たり判定があった場合はいろいろする
                       while (keyEventList.Count != 0)
                       {
                           var kvpair = keyEventList.Dequeue();
                           double min = double.MaxValue;  // 最短距離
                           BMObject minAt = null;  // 最短距離にあるオブジェ

                           for (int i = hitzoneLeft; i < hitzoneRight; i++)
                           {
                               var obj = b.PlayableBMObjects[i];
                               var dif = Math.Abs(obj.Seconds - CurrentSongPosition());
                               if (dif < min &&
                                   obj.Broken == false &&
                                   (obj.BMSChannel - 36) % 72 == kvpair.keyid)
                               {
                                   min = dif;
                                   minAt = b.PlayableBMObjects[i];
                               }
                           }

                           // オブジェの破壊が起きた
                           if (minAt != null)
                           {
                               Judgement judge = regulation.SecondsToJudgement(min);
                               if (judge != Judgement.None)  // 判定無しでなければ
                               {
                                   minAt.Broken = true;
                                   minAt.Judge = regulation.SecondsToJudgement(min);
                                   minAt.BrokeAt = kvpair.seconds;

                                   ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                   ps.TotalExScore += regulation.JudgementToScore(minAt.Judge);

                                   ps.CurrentMaximumAcceptance += 1;
                                   ps.TotalAcceptance += ((minAt.Judge >= Judgement.Good) ? 1 : 0);
                               }
                           }
                       }
                       #endregion

                       #region Skinクラスを使用した描画
                       skin.DrawBack(rt, b, ps);

                       // キーフラッシュ
                       foreach (var x in lastKeyEventDict)
                       {
                           skin.DrawKeyFlash(rt, b, ps,
                               new KeyEvent { keyid = x.Key, seconds = x.Value });
                       }

                       // 音符
                       for (int i = bombzoneLeft; i < right; i++)
                       {
                           var x = b.PlayableBMObjects[i];
                           if (x.Beat >= PlayFrom)
                           {
                               skin.DrawNote(rt, b, ps, x);
                           }
                       }

                       skin.DrawFront(rt, b, ps);
                       #endregion
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
                                && File.Exists(b.ToFullPath(fn))
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
                                        sbuf = new BitmapData(hdraw.HatoRenderTarget, b.ToFullPath(fn));

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
