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
    public class SimpleChipSkin : Skin
    {
        private BitmapData stagefile;
        private BitmapData chip;
        private BitmapData bomb;
        private BitmapData bar;
        private BitmapData bar_white;
        private BitmapData judgement;
        private BitmapData font;

        float ttt = 1.125f;

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

        Dictionary<int, BMObject> keyidToLastJudge;

        public SimpleChipSkin()
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

        public override void Load(RenderTarget rt, BMSStruct b)
        {
            myHS = HiSpeed;

            if (b.Stagefile != null && File.Exists(b.ToFullPath(b.Stagefile)))
            {
                stagefile = new BitmapData(rt, b.ToFullPath(b.Stagefile));
            }

            chip = new BitmapData(rt, HatoPath.FromAppDir("chip1.png"));
            bomb = new BitmapData(rt, HatoPath.FromAppDir("bomb1.png"));
            bar = new BitmapData(rt, HatoPath.FromAppDir("bar1.png"));
            bar_white = new BitmapData(rt, HatoPath.FromAppDir("bar1_white.png"));

            judgement = new BitmapData(rt, HatoPath.FromAppDir("judgement.png"));
            font = new BitmapData(rt, HatoPath.FromAppDir("font1.png"));
        }

        public override void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
            myHS += (HiSpeed - myHS) * 0.10;

            keyidToLastJudge = new Dictionary<int, BMObject>();

            if (stagefile != null)
            {
                rt.DrawBitmap(stagefile, 0f, 0f, 0.10f, 480f / stagefile.Height);
            }

            #region キーの後ろの灰色部分
            foreach (var bmschannel in new int[] { 36 + 1, 36 + 3, 36 + 5, 36 + 9 })
            {
                var xpos = 40f + ObjectPosX[(bmschannel + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;

                rt.FillRectangle(
                                xpos + 6 - 32f + 16f + 96f - 16f, 0,
                                36, 480,
                                0x888888, 0.12f);
            }
            #endregion

            #region ゲージ・スコアレート表示
            {
                // 受け入れレート
                float scorerate = ps.TotalAcceptance / (float)ps.MaximumAcceptance;
                float maxrate = ps.CurrentMaximumAcceptance / (float)ps.MaximumAcceptance;
                float scorepixel = (480 - 276 - 20) * scorerate;
                float maxpixel = (480 - 276 - 20) * maxrate;

                rt.FillRectangle(780f - 40f, 276f, 30f, 480 - 276 - 20, 0x666666);
                rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - maxpixel, 30f, maxpixel, 0x884444);
                rt.FillRectangle(780f - 40f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, 0xFF8888);

                rt.DrawText(font, "Gauge:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 300, 1.0f);
            }

            {
                // スコアレート
                float scorerate = ps.TotalExScore / (float)ps.MaximumExScore;
                float maxrate = ps.CurrentMaximumExScore / (float)ps.MaximumExScore;
                float scorepixel = (480 - 276 - 20) * scorerate;
                float maxpixel = (480 - 276 - 20) * maxrate;

                rt.FillRectangle(780f, 276f, 30f, 480 - 276 - 20, 0x666666);
                rt.FillRectangle(780f, 276f + (480 - 276 - 20) - maxpixel, 30f, maxpixel, 0x448844);
                rt.FillRectangle(780f, 276f + (480 - 276 - 20) - scorepixel, 30f, scorepixel, 0x88FF88);

                rt.DrawText(font, "Rate:\n" + Math.Floor(scorerate * 1000.0) / 10 + "%", 650, 400f, 1.0f);
            }
            #endregion

            rt.DrawBitmapSrc(bar,
                40f + 180 * ttt * 0.8f - 256f + 16f, + 420f - 16f,
                0, 0,
                512, 32,
                (float)0.5f);
        }

        public override void DrawKeyFlash(RenderTarget rt, BMSStruct b, PlayingState ps, KeyEvent obj)
        {
            var xpos = 40f + ObjectPosX[(obj.keyid + 36 + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f - 72f;
            var displacement = ps.Current.Seconds - obj.seconds;  // >= 0

            rt.FillRectangle(
                            xpos + 6 - 32f + 16f + 96f - 16f, 0,
                            36, 480,
                            0x00AAFF, (float)Math.Exp(-6 * displacement) * 0.20f);
        }

        public override void DrawNote(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject x)
        {
            float displacement = (float)(ps.Current.Disp - x.Disp);  // >= 0
            //int idx = (int)Math.Floor((b.transp.DispToSeconds(JustDisplacement) - x.Seconds) * 30) + 1;
            int idx = 0;
            var xpos = 40f + ObjectPosX[(x.BMSChannel + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f;
            var xpos2 = 40f + 180 * ttt * 0.8f;

            if (x.Broken)
            {
                // キー押し下しがあった
                idx = (int)Math.Floor((ps.Current.Seconds - x.BrokeAt) * 60) + 1;
            }

            var col = ((x.BMSChannel % 36 == 0x06) ? 2 : x.BMSChannel % 2);

            if (x.Terminal == null)
            {
                if (!x.Broken)
                {
                    // キー押し下しがなかった場合の処理

                    if (displacement > 0) displacement *= 0.25f;

                    rt.DrawBitmapSrc(chip,
                        xpos - 32f + 16f, displacement * (float)myHS * 360 + 420f - 32f,
                        col * 64, x.IsLandmine() ? 192 : 0,
                        64, 64);
                }
                else if (x.Judge <= Judgement.Bad)
                {
                    // BAD判定だった

                    if (displacement > 0) displacement *= 0.25f;

                    rt.DrawBitmapSrc(chip,
                        xpos - 32f + 16f, displacement * (float)myHS *  360 + 420f - 32f,
                        col * 64, x.IsLandmine() ? 192 : 0,
                        64, 64, 0.3f);
                }
                
                if(x.Broken)
                {
                    if (idx < 32)
                    {
                        // keyは、soundbmobjectのインデックス。まあuniqueなら何でもいい
                        int score = (int)x.Judge - 1;  // 0,1,2,3. 3:pg;
                        if (score < 0) score = 0;
                        
                        if (x.Judge >= Judgement.Bad)
                        {
                            /*
                            // 判定文字描画
                            rt.DrawBitmapSrc(judgement,
                                xpos - 64f + 16f, -128 + 420f - 32f + 39f,
                                idx / 2 % 2 * 128, (3 - score) * 64,
                                128, 64,
                                1.0f, 1.0f);
                             */
                            keyidToLastJudge[x.Keyid] = x;
                        }
                        if (idx < 16 && x.Judge >= Judgement.Good)
                        {
                            // ボム描画
                            rt.DrawBitmapSrc(bomb,
                                xpos - 64f + 16f - 64, +420f - 104f + 39f - 64,
                                idx % 4 * 128, idx / 4 * 128,
                                128, 128,
                                1.0f, 2.0f);
                        }
                    }
                }
            }
            else
            {
                float opac;
                float length;

                if (!x.Broken || x.Judge <= Judgement.Bad)
                {
                    // キー押し下しがなかった場合の処理
                    if (displacement > 0) displacement *= 0.25f;

                    opac = 1.0f;
                    length = (float)(x.Terminal.Disp - x.Disp);  // >0
                    if (displacement > 0)
                    {
                        //opac = 0.5f;
                        if (ps.Current.Seconds - x.Seconds > 0.1) opac = 0.3f;
                        displacement = 0;
                        length = (float)(x.Terminal.Disp - ps.Current.Disp);  // >0?
                        //if (length < 0) length = 0;
                        // FIXME: lengthの修正
                    }
                }
                else
                {
                    opac = 1.0f - 0.1f * (float)Math.Sin(2 * 3.14 * 10 * ps.Current.Seconds);
                    displacement = 0;
                    length = (float)(x.Terminal.Disp - ps.Current.Disp);  // >0?
                }

                float y_001 = (float)Math.Round((displacement - length) * myHS * 360 + 420f - 32f + 32);
                float y_002 = (float)Math.Round(displacement * myHS * 360 + 420f - 32f + 32);

                float yL = Math.Min(y_001, y_002);
                float yH = Math.Max(y_001, y_002);

                if (length >= 0)
                {
                    rt.DrawBitmapSrc(chip,
                        xpos - 32f + 16f, yL - 37,
                        col * 64, 64,
                        64, 37,
                        opac);
                    rt.DrawBitmapRect(chip,
                        xpos - 32f + 16f, yL,
                        64, yH - yL,
                        col * 64, 101,
                        64, 54,
                        opac);
                    rt.DrawBitmapSrc(chip,
                        xpos - 32f + 16f, yH,
                        col * 64, 155,
                        64, 37,
                        opac);
                }
            }
        }

        public override void DrawBarLine(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj)
        {
            throw new NotImplementedException();
        }

        public override void DrawFront(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
            foreach (var kvpair in keyidToLastJudge)
            {
                var x = kvpair.Value;
                var xpos = 40f + ObjectPosX[(x.BMSChannel + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f;
                int idx = (int)Math.Floor((ps.Current.Seconds - x.BrokeAt) * 60) + 1;
                int score = (int)x.Judge - 1;  // 0,1,2,3. 3:pg;
                if (score < 0) score = 0;
                
                rt.DrawBitmapSrc(judgement,
                    xpos - 64f + 16f, -128 + 420f - 32f + 39f,
                    idx / 2 % 2 * 128, (3 - score) * 64,
                    128, 64,
                    1.0f, 1.0f);
            }
            keyidToLastJudge = null;
        }
    }
}
