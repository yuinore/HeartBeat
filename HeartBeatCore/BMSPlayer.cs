﻿using HatoBMSLib;
using HatoLib;
using HatoDraw;
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
    public class BMSPlayer : IDisposable
    {
        //******************************************//
        // BMS読み込みオプション

        /// <summary>
        /// 先読みする時間差。曲の開始前に読み込む時間もここで定まる。
        /// キー音の演奏可能時刻からの差分ではなく適正演奏時間からの差分であるため、注意が必要。
        /// 1.0くらいで読み込みミスが発生しないくらいがちょうどいいと思う。
        /// (TODO:曲開始前のキー音割り当て)
        /// </summary>
        double WavFileLoadingDelayTime = 999;//10.0;

        /// <summary>
        /// 読み込み完了から曲が再生されるまでの時間
        /// </summary>
        double DelayingTimeBeforePlay = 3.0;//1.0;

        /// <summary>
        /// 曲の再生を開始する地点（秒）
        /// </summary>
        double PlayFrom = 0.0;

        /// <summary>
        /// 楽曲の開始前にファイル読み込みを行うwavファイルサイズ
        /// </summary>
        long PreLoadingWaveFileSize = 524288;

        /// <summary>
        /// 楽曲の開始前にファイル読み込みを行うoggファイルサイズ
        /// </summary>
        long PreLoadingOggFileSize = 100000;

        /// <summary>
        /// 楽曲の開始前にファイル読み込みを行うフェーズのタイムアウト時間。
        /// HDDの速度が極端に遅かったりするとタイムアウトする。
        /// </summary>
        int PreLoadingTimeoutMilliSeconds = 40000;//20000;

        /// <summary>
        /// LN終端で、早く離しても接続される時間。GOOD判定の時間と等しい値にするのが良い。
        /// </summary>
        double LNTerminalJudgeSeconds = 0.12;

        //******************************************//
        // 設定可能なプロパティ

        public bool Playside2P = false;

        public bool autoplay = false;

        // TODO: Skinへの移行
        public float RingShowingPeriodByMeasure
        {
            get { return skin.RingShowingPeriodByMeasure; }
            set { skin.RingShowingPeriodByMeasure = value; }
        }

        double tempRHS = 1.0;

        bool fast;  // fast再生だとさすがに間に合わなかったり。
        public bool Fast
        {
            get { return fast; }
            set
            {
                if (fast != value)
                {
                    if (value)
                    {
                        CurrentSongPosition();  // 経過時間の更新
                        tempRHS *= 5.0;
                        WavFileLoadingDelayTime *= 5.0;
                    }
                    else
                    {
                        CurrentSongPosition();  // 経過時間の更新
                        tempRHS /= 5.0;
                        WavFileLoadingDelayTime /= 5.0;
                    }
                    fast = value;
                }
            }
        }

        public double UserHiSpeed
        {
            get { return skin.UserHiSpeed; }
            set { skin.UserHiSpeed = value; }
        }

        //******************************************//

        private BMSStruct b;  // ガベージコレクタに回収されてはならないんだぞ（←は？）

        // TODO: これ↓の初期化処理
        private GameRegulation regulation = new HeartBeatRegulation();

        // TODO: これ↓の初期化処理
        public Skin skin = new SimpleChipSkin();

        PlayingState ps = new PlayingState();  // スキンにデータを受け渡すのに使う

        InputHandler ih;

        HatoDrawDevice hdraw;
        HatoPlayerDevice hplayer;

        Form form;

        int countdraw = 0;

        BitmapData font = null;
        Action<RenderTarget> onPaint;

        BitmapData bga_front = null;
        BitmapData bga_back = null;
        BitmapData bga_poor = null;

        Stopwatch s = new Stopwatch();

        StringBuilder ConsoleMessage = new StringBuilder("Waiting...\n");
        string LineMessage = "\n";

        private void TraceMessage(string text, bool cons = true)
        {
            ConsoleMessage.Insert(0, text + "\n");

            if (cons)
            {
                Console.WriteLine(text);
            }
        }
        private void TraceWarning(string text, bool cons = true)
        {
            ConsoleMessage.Insert(0, text + "\n");

            if (cons)
            {
                Console.WriteLine(text);
            }
        }

        /// <summary>
        /// 同期的にフォームを開きます。
        /// Run()を呼び出すまで、レンダリングループは開始されません。
        /// </summary>
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

            form = hdraw.OpenForm();

            return form;
        }

        /// <summary>
        /// レンダーループを同期的に開始します。
        /// DirectXフォームが閉じられると、処理が返ります。
        /// </summary>
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
                   if (s != null && s.ElapsedMilliseconds != 0)
                   {
                       countdraw++;
                       LineMessage = "Ave " + Math.Round(countdraw * 1000.0 * 10 / s.ElapsedMilliseconds) / 10.0 + "fps\n" +
                           "m=" + String.Format("{0:0.00}", (double)b.transp.BeatToMeasure(b.transp.SecondsToBeat(CurrentSongPosition()))) +
                           ", b=" + String.Format("{0:0.00}", b.transp.SecondsToBeat(CurrentSongPosition())) +
                           ", t=" + String.Format("{0:0.00}", CurrentSongPosition()) +
                           ", d=" + String.Format("{0:0.00}", b.transp.BeatToDisplacement(b.transp.SecondsToBeat(CurrentSongPosition()))) + "s\n";
                   }

                   rt.ClearBlack();

                   if (onPaint != null)
                   {
                       onPaint(rt);
                   }

                   try
                   {
                       rt.DrawText(font, LineMessage + ConsoleMessage.ToString(), 4, 4);  // tostringいらない！？
                   }
                   catch
                   {
                   }
               });
        }

        double lastelapsed = 0;
        double sumelapsed = 0;
        List<int> songposLock = new List<int>();
        internal double CurrentSongPosition()
        {
            double ret;
            lock (songposLock)  // ←超超超超超重要（というかそんなにみんなCurrentSongPosition()呼んでるんですか？？）
            {
                double ms = s.ElapsedMilliseconds;
                sumelapsed += (ms - lastelapsed) * tempRHS / 1000.0;
                lastelapsed = ms;
                ret = sumelapsed;
            }
            return ret + PlayFrom - DelayingTimeBeforePlay;
        }

        /// <summary>
        /// BMSファイルを読み込んで、直ちに再生します。
        /// Run()より前に呼んでも後に呼んでも構いませんが、
        /// どちらにしてもRun()を呼び出す必要があります多分。
        /// </summary>
        public async void LoadAndPlay(string path, int startmeasure = 0)
        {
            {
                Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
            }

            s = new Stopwatch();

            // キーに割り当てられているキー音のwavid
            Dictionary<int, int> keysound = new Dictionary<int, int>();

            // ロングノート演奏中のオブジェ
            Dictionary<int, BMObject> holdingObject = new Dictionary<int, BMObject>();
            bool keysoundReady = false;

            ih = new InputHandler(this, form);
            ih.KeyDown += (o, keyid) =>
            {
                int wavid;

                if (keysoundReady && keysound.TryGetValue(keyid, out wavid))
                {
                    if (!hplayer.PlaySound(wavid, true))
                    {
                        if (b.WavDefinitionList.ContainsKey(wavid))
                        {
                            TraceWarning("  Warning : Audio \"" + b.WavDefinitionList.GetValueOrDefault(wavid) + "\" (invoked by key input) is not loaded yet...");
                        }
                        else
                        {
                            // WAVが定義されていない場合。
                            // （空文字定義の場合は除く）
                        }
                    }
                }
            };
            ih.KeyUp += (o, keyid) =>
            {
            };
            // TODO: LoadAndPlayが2回呼ばれると、イベントハンドラが重複登録されたりしないかどうかチェック

            b = new BMSStruct(path);
            TraceWarning(b.Message, false);

            ps.MaximumAcceptance = b.PlayableBMObjects.Count();
            ps.MaximumExScore = ps.MaximumAcceptance * regulation.MaxScorePerObject;

            foreach (var x in b.SoundBMObjects)
            {
                if(x.BMSChannel != 0x01 && !keysound.ContainsKey(x.Keyid)) {
                    keysound[x.Keyid] = x.Wavid;
                }
            }

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
                TraceMessage("NOTES COUNT: " + b.PlayableBMObjects.Where(x => x.IsPlayable() && !x.IsLongNoteTerminal && !x.IsLandmine()).Count());
                TraceMessage("TOTAL: " + b.Total);
            }

            var dictbmp = new Dictionary<int, BitmapData>();

            TraceMessage("Timer Started.");

            double tempomedian = b.CalcTempoMedian(0.667);  // 3分の2くらいが低速だったらそっちに合わせようかな、という気持ち
            skin.BaseHiSpeed = (150.0 / tempomedian);

            Func<double, double> PosOrZero = (x) => (x < 0) ? 0 : x;

            #region PlayFromとDelayingTimeBeforePlayの調整
            PlayFrom = b.transp.MeasureToSeconds(new Rational(startmeasure));

            // ↓謎のコードその1 (演奏開始時刻を早めるタイプの最適化)
            {
                var b1 = b.SoundBMObjects.FirstOrDefault();
                var b2 = b.GraphicBMObjects.FirstOrDefault();
                if (b1 != null || b2 != null)
                {
                    PlayFrom = Math.Max(
                        Math.Min(
                            b1 == null ? 99 : b1.Seconds,
                            b2 == null ? 99 : b2.Seconds),
                        PlayFrom);
                }
                // あんまり変なことすると例えばBGAがあった時とかどうするのよ
                // 自然が一番なんじゃないの？？
            }

            // ↓謎のコードその2 (演奏開始時刻を遅くするタイプの最適化)
            DelayingTimeBeforePlay = Math.Max(DelayingTimeBeforePlay, 
                autoplay 
                    ? 1.0  // オートプレイなら1秒後に開始
                    : (b.PlayableBMObjects.Count >= 1  // 演奏可能オブジェがある前提で、
                        ? Math.Max(1.0, 3.0 - PosOrZero(b.PlayableBMObjects[0].Seconds - PlayFrom))  // 最初の演奏可能ノーツが現れるまでに3秒掛かるようにする
                        : 1.0));

            TraceMessage("PlayFrom = " + Math.Round(PlayFrom * 1000) + "ms, Delay = " + Math.Round(DelayingTimeBeforePlay * 1000) + "ms");
            #endregion

            Stopwatch loadingTime = new Stopwatch();
            loadingTime.Start();

            #region HatoPlayerの初期化とシンセの初期化
            if (hplayer == null)  // ←？？？？？
            {
                hplayer = new HatoPlayerDevice(form, b);  // thisでもいいのか？
            }

            hplayer.b = b;

            foreach (var kvpair in b.SynthDefinitionList)
            {
                hplayer.PrepareSynth(kvpair.Key, kvpair.Value);
            }

            //hplayer.Run();
            #endregion

            #region プリローディング
            {
                // ああ、StartNewすればいいのか・・・
                Task task = Task.Factory.StartNew(() =>
                {
                    Parallel.ForEach(keysound, (kvpair) =>
                    {
                        hplayer.PrepareSound(kvpair.Value);
                    });

                    Parallel.ForEach(b.SoundBMObjects, (sb) =>
                    { 
                        // 等号が入るかどうかに注意な！
                        if (sb.Seconds >= PlayFrom && sb.Seconds <= PlayFrom + WavFileLoadingDelayTime)
                        {
                            hplayer.PrepareSound(sb.Wavid);
                        }
                        else if (b.WavDefinitionList.ContainsKey(sb.Wavid))
                        {
                            var fn = AudioFileReader.FileName(b.ToFullPath(b.WavDefinitionList[sb.Wavid]));
                            
                            if (fn != null &&
                                ((Path.GetExtension(fn).ToLower() == ".wav" && (new FileInfo(fn)).Length >= PreLoadingWaveFileSize) ||
                                (Path.GetExtension(fn).ToLower() == ".ogg" && (new FileInfo(fn)).Length >= PreLoadingOggFileSize)))  // 512kB以上なら先読み
                            {
                                hplayer.PrepareSound(sb.Wavid);
                            }
                        }
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

            keysoundReady = true;

            loadingTime.Stop();

            TraceMessage("    Loading Time: " + loadingTime.ElapsedMilliseconds + "ms");

            hplayer.Run();  // ASIOデバイスを起動して再生する

            s.Start();  // 内部タイマーの作動

            // ↓約150行
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
                           Seconds = CurrentSongPosition(),
                           Measure = 0
                       };
                       current.Beat = b.transp.SecondsToBeat(current.Seconds);
                       current.Disp = b.transp.BeatToDisplacement(current.Beat);

                       // PlayingStateの設定
                       ps.Current = current;
                       ps.LastKeyDownEvent = ih.LastKeyDownEvent;

                       double AppearDisplacement = current.Disp + 4.0;

                       #region オブジェ範囲の更新
                       for (; left < b.PlayableBMObjects.Count; left++)  // 視界から消える箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[left];
                           if (x.Disp >= current.Disp - skin.EyesightDisplacementAfter) break;
                       }

                       for (; right < b.PlayableBMObjects.Count; right++)  // 視界に出現する箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[right];
                           if (x.Disp >= current.Disp + skin.EyesightDisplacementBefore) break;
                       }

                       for (; hitzoneLeft < b.PlayableBMObjects.Count; hitzoneLeft++)  // 判定ゾーンから消える箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[hitzoneLeft];
                           if (x.Seconds + regulation.JudgementWindowSize >= current.Seconds) break;
                           else
                           {
                               lock (x)
                               {
                                   // 判定ゾーンから外にオブジェクトが出ます
                                   if (x.Broken == false)
                                   {
                                       x.Broken = true;
                                       x.Judge = Judgement.None;
                                       x.BrokeAt = CurrentSongPosition();

                                       ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                       ps.CurrentMaximumAcceptance += 1;

                                       if (x.Terminal != null)
                                       {
                                           lock (x.Terminal)
                                           {
                                               // LNつながりません
                                               x.Terminal.Broken = true;
                                               x.Terminal.Judge = Judgement.None;
                                               x.Terminal.BrokeAt = CurrentSongPosition();
                                           }
                                       }
                                   }
                               }
                           }
                       }

                       for (; hitzoneRight < b.PlayableBMObjects.Count; hitzoneRight++)  // 判定ゾーンに突入する箇所、left <= right
                       {
                           var x = b.PlayableBMObjects[hitzoneRight];
                           if (x.Seconds - regulation.JudgementWindowSize >= current.Seconds) break;
                       }

                       // TODO: もし曲の最初から最後までの長さがあるLNがあった場合は？？
                       for (; bombzoneLeft < b.PlayableBMObjects.Count; bombzoneLeft++)  // ボム・キーフラッシュが消える箇所
                       {
                           var x = b.PlayableBMObjects[bombzoneLeft];
                           if ((x.Terminal ?? x).Seconds + (regulation.JudgementWindowSize + skin.BombDuration) >= current.Seconds) break;
                       }
                       #endregion

                       #region LN終端判定
                       lock (holdingObject)
                       {
                           List<int> removeList = new List<int>();
                           foreach (var x in holdingObject)
                           {
                               if (x.Value.Terminal.Seconds <= current.Seconds)
                               {
                                   lock (x.Value.Terminal)
                                   {
                                       if (x.Value.Terminal.Broken)
                                       {
                                           Console.WriteLine("おかしいぞ？");
                                       }
                                       x.Value.Terminal.Broken = true;
                                       x.Value.Terminal.Judge = Judgement.Perfect;
                                       x.Value.Terminal.BrokeAt = x.Value.Terminal.Seconds;

                                       ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                       ps.TotalExScore += regulation.JudgementToScore(x.Value.Judge);

                                       ps.CurrentMaximumAcceptance += 1;
                                       ps.TotalAcceptance += ((x.Value.Judge >= Judgement.Good) ? 1 : 0);  // 常に1を返す

                                       removeList.Add(x.Key);
                                   }
                               }
                           }

                           foreach (var x in removeList)
                           {
                               holdingObject.Remove(x);
                           }
                       }
                       #endregion

                       // キー入力キューの消化は描画処理じゃないと思うんですがそれは・・・
                       // 　→判定処理は、オブジェに時間幅があるので、ここで処理するのが良い。
                       #region キー入力キュー(ih.KeyEventList)の消化試合
                       // キー入力に最近のオブジェを探す
                       // 当たり判定があった場合はいろいろする
                       lock (ih.KeyEventList)
                       {
                           while (ih.KeyEventList.Count != 0)
                           {
                               var kvpair = ih.KeyEventList.Dequeue();
                               double min = double.MaxValue;  // 最短距離
                               BMObject minAt = null;  // 最短距離にあるオブジェ

                               if (kvpair.IsKeyUp == false)
                               {
                                   // キーダウン
                                   for (int i = hitzoneLeft; i < hitzoneRight; i++)
                                   {
                                       var obj = b.PlayableBMObjects[i];
                                       var dif = Math.Abs(obj.Seconds - kvpair.seconds);
                                       if (dif < min &&
                                           obj.Broken == false &&
                                           obj.Keyid == kvpair.keyid)
                                       {
                                           min = dif;
                                           minAt = b.PlayableBMObjects[i];
                                       }
                                   }

                                   // オブジェの破壊が起きた
                                   if (minAt != null)
                                   {
                                       lock (minAt)
                                       {
                                           Judgement judge = regulation.SecondsToJudgement(min);
                                           if (judge != Judgement.None)  // 判定無しでなければ
                                           {
                                               minAt.Broken = true;
                                               minAt.Judge = regulation.SecondsToJudgement(min);
                                               minAt.BrokeAt = kvpair.seconds;

                                               if (minAt.Terminal == null)
                                               {
                                                   // LNではないなら
                                                   ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                                   ps.TotalExScore += regulation.JudgementToScore(minAt.Judge);

                                                   ps.CurrentMaximumAcceptance += 1;
                                                   ps.TotalAcceptance += ((minAt.Judge >= Judgement.Good) ? 1 : 0);
                                               }
                                               else
                                               {
                                                   // LNなら

                                                   if (minAt.Judge >= Judgement.Good)
                                                   {
                                                       // LNが繋がりそう
                                                       // FIXME: 既にLN押してる状態だと例外が出そう
                                                       holdingObject.Add(minAt.Keyid, minAt);
                                                   }
                                                   else
                                                   {
                                                       lock (minAt.Terminal)
                                                       {
                                                           // LNつながりません
                                                           minAt.Terminal.Broken = true;
                                                           minAt.Terminal.Judge = Judgement.None;
                                                           minAt.Terminal.BrokeAt = kvpair.seconds;

                                                           ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                                           ps.CurrentMaximumAcceptance += 1;
                                                       }
                                                   }
                                               }
                                           }
                                       }
                                   }
                               }
                               else
                               {
                                   // キーアップ
                                   if (holdingObject.ContainsKey(kvpair.keyid))
                                   {
                                       var begin = holdingObject[kvpair.keyid];
                                       var term = begin.Terminal;

                                       lock (term)
                                       {
                                           bool OK = term.Seconds - LNTerminalJudgeSeconds <= kvpair.seconds;  // LN繋いだ？

                                           term.Broken = true;
                                           term.Judge = OK ? Judgement.Perfect : Judgement.None;
                                           term.BrokeAt = kvpair.seconds;

                                           ps.CurrentMaximumExScore += regulation.MaxScorePerObject;
                                           ps.TotalExScore += regulation.JudgementToScore(OK ? begin.Judge : Judgement.None);

                                           ps.CurrentMaximumAcceptance += 1;
                                           ps.TotalAcceptance += OK ? 1 : 0;  // LN切ってしまった

                                           holdingObject.Remove(kvpair.keyid);
                                       }
                                   }
                               }
                           }
                       }
                       #endregion

                       #region Skinクラスを使用した描画
                       // 背景・初期化処理
                       skin.DrawBack(rt, b, ps);

                       // キーフラッシュ
                       lock (ih.LastKeyDownEventDict)
                       {
                           foreach (var x in ih.LastKeyDownEventDict)  // 例外：コレクションが変更されました。列挙操作は実行されない可能性があります。
                           {
                               skin.DrawKeyFlash(rt, b, ps,
                                   new KeyEvent { keyid = x.Key, seconds = x.Value });
                           }
                       }

                       // 音符
                       for (int i = bombzoneLeft; i < right; i++)
                       {
                           var x = b.PlayableBMObjects[i];
                           if (x.Seconds >= PlayFrom)
                           {
                               skin.DrawNote(rt, b, ps, x);
                           }
                       }

                       // 前景・終了処理
                       skin.DrawFront(rt, b, ps);
                       #endregion

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
                   };
            }
            #endregion

            // ↓約200行
            #region wav/bmpの読み込み・再生
            //await Task.Run(() => // それぞれのラムダ式の中のTask.Delayにawaitが付いていることで、非同期に実行できる。
            Parallel.Invoke(
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
                            // awaitしない！！ するな！！
                            // って思ったけど、awaitしなきゃいけない程重要な処理じゃないんですよね・・・
                            // できればスレッドの優先度をBMPも含めて下げたい
                            await Task.Run(() => hplayer.PrepareSound(x.Wavid));
                            // Parallel.Invoke(async () => await Task.Run(() => hplayer.PrepareSound(x.Wavid)));
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
                                
                                //Parallel.Invoke(async () => await Task.Run(() =>
                                await Task.Run(() =>
                                {
                                    try
                                    {
                                        string[] staticimageExt = { ".bmp", ".png", ".gif", ".tiff", ".jpg" };  // 動画ファイルが除外できれば何でもいい

                                        string ext = Path.GetExtension(fn).ToLower();

                                        if (staticimageExt.Contains(ext))
                                        {
                                            sbuf = new BitmapData(hdraw.HatoRenderTarget, b.ToFullPath(fn));

                                            lock (dictbmp)
                                            {
                                                dictbmp[x.Wavid] = sbuf;
                                                //TraceMessage("    " + b.WavDefinitionList[x.Wavid] + " Load Completed (" + dict.Count + "/" + b.WavDefinitionList.Count + ")");
                                            }
                                        }
                                        else
                                        {
                                            // 動画ファイルの可能性が大きい
                                            TraceWarning("  Unknown File Format: " + fn);
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

                        if (x.Seconds >= PlayFrom && (x.IsPlayable() || x.IsInvisible()))  // autoplayかどうかによらない、また、非表示でもOK
                        {
                            keysound[x.Keyid] = x.Wavid;
                        }

                    }
                },
                async () =>  // wavの再生（含オートプレイ）
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
                            if (autoplay ? !x.IsInvisible() : x.IsBackSound())
                            {
                                if (!hplayer.PlaySound(x.Wavid, false))
                                {
                                    if (b.WavDefinitionList.ContainsKey(x.Wavid))
                                    {
                                        TraceWarning("  Warning : Audio \"" + b.WavDefinitionList.GetValueOrDefault(x.Wavid) + "\" is not loaded yet...");
                                    }
                                    else
                                    {
                                        // WAVが定義されていない場合。
                                        // （空文字定義の場合は除く）
                                    }
                                }
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
                                TraceWarning("  Warning : Graphic " + b.BitmapDefinitionList.GetValueOrDefault(x.Wavid) + " is not loaded yet...");
                            }
                        }
                    }
                },
                async () =>  // オートプレイ
                {
                    foreach (var x in b.PlayableBMObjects)
                    {
                        while (x.Seconds - 0.0025 >= CurrentSongPosition())
                        {
                            await Task.Delay(10);
                        }

                        if (x.Seconds >= PlayFrom && autoplay && !x.IsLandmine())
                        {
                            //double CSP = CurrentSongPosition();  // ベンチマーク？用
                            double CSP = x.Seconds;  // 見栄え優先で行こう

                            ih.LastKeyDownEvent = new KeyEvent
                            {
                                keyid = x.Keyid,
                                seconds = CSP
                            };
                            lock (ih.LastKeyDownEventDict)
                            {
                                ih.LastKeyDownEventDict[x.Keyid] = CSP;
                            }

                            lock (ih.KeyEventList)
                            {
                                ih.KeyEventList.Enqueue(ih.LastKeyDownEvent);
                            }

                            // ここではキー音の再生はしない
                        }
                    }
                });
            //);
            #endregion
        }

        public void Stop()
        {
        }

        #region implementation of IDisposable
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            
            System.Diagnostics.Debug.Assert(disposing, "激おこ");

            if (disposing)
            {
                // Free any other managed objects here.
                if (hplayer != null)
                {
                    hplayer.Dispose();
                    hplayer = null;
                }
            }

            // Free any unmanaged objects here.

            disposed = true;
        }
        
        ~BMSPlayer()
        {
            Dispose(false);
        }
        #endregion
    }
}
