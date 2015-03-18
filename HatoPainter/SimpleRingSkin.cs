using HatoBMSLib;
using HatoDraw;
using HatoLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoPainter
{
    public class SimpleRingSkin : Skin
    {
        private BitmapData stagefile;
        private BitmapData ring1;
        private BitmapData ring2;
        private BitmapData ring3;
        private BitmapData bar;
        private BitmapData bar_white;
        private BitmapData judgement;
        private BitmapData font;

        float ttt = 1.5f;

        double myHS;

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

        public SimpleRingSkin()
        {
            // 親クラスのフィールドの初期化
            RingShowingPeriodByMeasure = 2.0f;
        }

        public override double BombDuration
        {
            get { return 2.0; }
        }

        public override double EyesightDisplacementBefore
        {
            get
            {
                if (myHS >= 0)
                {
                    return 4.0 / Math.Max(0.2, myHS);
                }
                else
                {
                    return 1.0 / Math.Max(0.2, -myHS);
                }
            }
        }

        public override double EyesightDisplacementAfter
        {
            get
            {
                if (myHS >= 0)
                {
                    return 1.0 / Math.Max(0.2, myHS);
                }
                else
                {
                    return 4.0 / Math.Max(0.2, -myHS);
                }
            }
        }

        private float MeasureToYPos(Rational m, float phase)
        {
            int stepN = 4;
            float RSP = RingShowingPeriodByMeasure;  // 長すぎる変数名はダメ
            float step = ((float)Math.Floor((((float)m + 1025 + phase * RSP) % (RSP * stepN)) / RSP) - (stepN - 1.0f) / 2) / stepN;  // おおよそ -0.5～0.5
            return (((float)m + 1025 + 0 * RSP) % RSP - 0 * RSP) / RSP + step * 0.20f;
        }

        public override void Load(RenderTarget rt, BMSStruct b)
        {
            myHS = UserHiSpeed;

            if (b.Stagefile != null && File.Exists(b.ToFullPath(b.Stagefile)))
            {
                stagefile = new BitmapData(rt, b.ToFullPath(b.Stagefile));
            }

            ring1 = new BitmapData(rt, HatoPath.FromAppDir("ring1.png"));
            ring2 = new BitmapData(rt, HatoPath.FromAppDir("ring2.png"));
            ring3 = new BitmapData(rt, HatoPath.FromAppDir("ring3.png"));
            bar = new BitmapData(rt, HatoPath.FromAppDir("bar1.png"));
            bar_white = new BitmapData(rt, HatoPath.FromAppDir( "bar1_white.png"));

            judgement = new BitmapData(rt, HatoPath.FromAppDir("judgement.png"));
            font = new BitmapData(rt, HatoPath.FromAppDir("font1.png"));
        }

        public override void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
            myHS += (UserHiSpeed - myHS) * 0.10;

            if (stagefile != null)
            {
                rt.DrawBitmap(stagefile, 0f, 0f, 0.10f, 480f / stagefile.Height);
            }

            #region キーの後ろの灰色部分
            foreach (var bmschannel in new int[] { 36 + 1, 36 + 3, 36 + 5, 36 + 9 })
            {
                var xpos = 40f + ObjectPosX[(bmschannel + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;

                rt.FillRectangle(
                                xpos - 32f + 16f + 96f - 16f, 0,
                                48, 480,
                                new ColorBrush(rt, 0x888888, 0.12f));
            }
            #endregion

            #region ゲージ・スコアレート表示
            {
                // 受け入れレート
                float scorerate = ps.TotalAcceptance / (float)ps.MaximumAcceptance;
                float maxrate = ps.CurrentMaximumAcceptance / (float)ps.MaximumAcceptance;
                float scorepixel = (480 - 276 - 20) * scorerate;
                float maxpixel = (480 - 276 - 20) * maxrate;

                rt.FillRectangle(780f - 40f, 276f, 30f, 480 - 276 - 20, new ColorBrush(rt, 0x666666));
                rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - maxpixel, 30f, maxpixel, new ColorBrush(rt, 0x884444));
                rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, new ColorBrush(rt, 0xFF8888));

                rt.DrawText(font, "Gauge:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 300, 1.0f);
            }

            {
                // スコアレート
                float scorerate = ps.TotalExScore / (float)ps.MaximumExScore;
                float maxrate = ps.CurrentMaximumExScore / (float)ps.MaximumExScore;
                float scorepixel = (480 - 276 - 20) * scorerate;
                float maxpixel = (480 - 276 - 20) * maxrate;

                rt.FillRectangle(780f, 276f, 30f, 480 - 276 - 20, new ColorBrush(rt, 0x666666));
                rt.FillRectangle(780f, 276f + (480 - 276 - 20) - maxpixel, 30f, maxpixel, new ColorBrush(rt, 0x448844));
                rt.FillRectangle(780f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, new ColorBrush(rt, 0x88FF88));

                rt.DrawText(font, "Rate:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 400f, 1.0f);
            }
            #endregion
        }

        public override void DrawKeyFlash(RenderTarget rt, BMSStruct b, PlayingState ps, KeyEvent obj)
        {
            var xpos = 40f + ObjectPosX[(obj.keyid + 36 + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;
            var displacement = ps.Current.Seconds - obj.seconds;  // >= 0

            rt.FillRectangle(
                            xpos - 32f + 16f + 96f - 16f, 0,
                            48, 480,
                            new ColorBrush(rt, 0x00AAFF, (float)Math.Exp(-6 * displacement) * 0.20f));
        }

        public override void DrawNote(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject x)
        {
            var posdisp = (ps.Current.Seconds - x.Seconds) * 2.4 * myHS;
            //var displacement = (ps.Current.Seconds - x.Seconds) * 1.2 / RingShowingPeriodByMeasure;  // >= 0
            //int idx = (int)Math.Floor((b.transp.BeatToSeconds(JustDisplacement) - x.Seconds) * 30) + 1;
            int idx = 0;
            var xpos = 40f + ObjectPosX[(x.BMSChannel + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f;
            var xpos2 = 40f + 180 * ttt * 0.8f;

            if (!x.Broken)
            {
                // キー押し下しがなかった場合の処理

                idx = (int)Math.Floor((ps.Current.Seconds - x.Seconds) * 30) + 1;
                var opac = (idx >= 0 ? (0.6f / (idx * 6f + 1f)) : 1.0f);

                if (posdisp > 0) posdisp = 0;

                var bmp = x.Terminal == null ? ring1 : ring3;
                float srcpos = x.Terminal == null ? 0 : 512 - 64;  // ひどい・・・

                //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                rt.DrawBitmapSrc(bmp,
                    xpos - 32f + 16f + (float)posdisp * 25f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 32f,
                    srcpos, srcpos,
                    64, 64,
                    (float)Math.Exp(+1.8 * posdisp) * 1.0f * opac);
                rt.DrawBitmapSrc(bmp,
                    xpos - 32f + 16f - (float)posdisp * 25f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 32f,
                    srcpos, srcpos,
                    64, 64,
                    (float)Math.Exp(+1.8 * posdisp) * 1.0f * opac);
                rt.DrawBitmapSrc(bar,
                    xpos2 - 256f + 16f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 16f,
                    0, 0,
                    512, 32,
                    (float)Math.Exp(+1.8 * posdisp) * 0.5f * opac);
            }
            else
            {
                if (x.Judge >= Judgement.Bad)
                {
                    // キー押し下しがあった場合（オブジェが通過した場合を***含まない****）の処理
                    if (x.Terminal == null)
                    {
                        idx = (int)Math.Floor((ps.Current.Seconds - x.BrokeAt) * 60) + 1;
                    }
                    else
                    {
                        idx = (int)Math.Floor((ps.Current.Seconds - x.Terminal.Seconds) * 60) + 1;
                    }

                    if (x.Terminal == null || x.Terminal.Seconds <= ps.Current.Seconds)
                    {
                        if (idx < 32)
                        {
                            var bmp = x.Terminal == null ? ring1 : ring2;

                            //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                            rt.DrawBitmapSrc(bmp,
                                xpos - 32f + 16f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 32f,
                                idx % 8 * 64, idx / 8 * 64,
                                64, 64,
                                1.0f);
                            rt.DrawBitmapSrc(bar,
                                xpos2 - 256f + 16f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 16f,
                                0, idx / 2 * 32,
                                512, 32,
                                0.1f);

                            // keyは、soundbmobjectのインデックス。まあuniqueなら何でもいい
                            int score = (int)x.Judge - 1;  // 0,1,2,3. 3:pg;
                            if (score < 0) score = 0;

                            rt.DrawBitmapSrc(judgement,
                                xpos - 64f + 16f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 32f + 39f,
                                idx / 2 % 2 * 128, (3 - score) * 64,
                                128, 64,
                                1.0f, 1.0f);
                        }
                    }
                    else
                    {
                        // ロングノート、かつ、現在時刻が終点より前
                        float opac2 = 0.1f - 0.01f * (float)Math.Sin(2 * 3.14 * 10 * ps.Current.Seconds);

                        double start = Math.Min(x.BrokeAt, x.Seconds);

                        if (x.Terminal.Beat - x.Beat < 2.0)
                        {
                            start = b.transp.BeatToSeconds(x.Terminal.Beat - 2);
                        }

                        idx = (int)((ps.Current.Seconds - start) / (x.Terminal.Seconds - start) * 64.0);
                        if (idx < 0) idx = 0;
                        if (idx >= 64) idx = 63;  // いらない

                        //rt.DrawBitmap(bomb, 30f + ObjectPosX[(x.BMSChannel - 36) % 72] - 72f, 400f - 40f, (float)Math.Exp(-3 * displacement) * 1.0f, 0.1f);
                        rt.DrawBitmapSrc(ring3,
                            xpos - 32f + 16f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 32f,
                            512 -  64, 512 - 64,
                            64, 64,
                            1.0f);
                        rt.DrawBitmapSrc(ring3,
                            xpos - 32f + 16f + ((1 - 1.4f) * 32), -MeasureToYPos(x.Measure, 0) * 320 + 400f - 32f + ((1 - 1.4f) * 32),
                            idx % 8 * 64, idx / 8 * 64,
                            64, 64,
                            1.0f,
                            1.4f);
                        rt.DrawBitmapSrc(bar,
                            xpos2 - 256f + 16f, -MeasureToYPos(x.Measure, 0) * 320 + 400f - 16f,
                            0, 0,
                            512, 32,
                            opac2);
                    }
                }
            }
        }

        public override void DrawBarLine(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj)
        {
            throw new NotImplementedException();
        }

        public override void DrawFront(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
            #region キー入力時間を示す白いバー（多分）
            while (ps.LastKeyEvent != null)
            {
                var xpos = 40f + ObjectPosX[(ps.LastKeyEvent.keyid + 36 + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;
                var xpos2 = 40f + 180 * ttt * 0.8f;
                var displacement = ps.Current.Seconds - ps.LastKeyEvent.seconds;  // >= 0
                var m = b.transp.BeatToMeasure(b.transp.SecondsToBeat(ps.LastKeyEvent.seconds));

                if (((int)(displacement * 30)) < 16)
                {
                    rt.DrawBitmapSrc(bar_white,
                        xpos2 - 256f + 16f, -(MeasureToYPos(m, +1) - 1) * 320 + 400f - 8f,
                        //0, 0 + 12, 
                        0, ((int)(displacement * 30)) * 16,
                        512, 16,
                        0.5f);
                }
                if (((int)(displacement * 30)) < 16)
                {
                    rt.DrawBitmapSrc(bar_white,
                        xpos2 - 256f + 16f, -MeasureToYPos(m, 0) * 320 + 400f - 8f,
                        //0, 0 + 12,
                        0, ((int)(displacement * 30)) * 16,
                        512, 16,
                        0.5f);
                }
                if (((int)(displacement * 30)) < 16)
                {
                    rt.DrawBitmapSrc(bar_white,
                        xpos2 - 256f + 16f, -(MeasureToYPos(m, -1) + 1) * 320 + 400f - 8f,
                        //0, 0 + 12,
                        0, ((int)(displacement * 30)) * 16,
                        512, 16,
                        0.5f);
                }
                break;
            }
            #endregion
        }
    }
}
