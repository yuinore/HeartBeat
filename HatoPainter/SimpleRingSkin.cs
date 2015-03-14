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
        private BitmapData bomb;
        private BitmapData bar;
        private BitmapData bar_white;
        private BitmapData judgement;

        float ttt = 1.5f;

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

            BombDuration = 2.0;
        }

        public override void Load(RenderTarget rt, BMSStruct b)
        {
            if (b.Stagefile != null && File.Exists(b.ToFullPath(b.Stagefile)))
            {
                stagefile = new BitmapData(rt, b.ToFullPath(b.Stagefile));
            }
            
            bomb = new BitmapData(rt, HatoPath.FromAppDir("ring1.png"));
            bar = new BitmapData(rt, HatoPath.FromAppDir("bar1.png"));
            bar_white = new BitmapData(rt, HatoPath.FromAppDir( "bar1_white.png"));

            judgement = new BitmapData(rt, HatoPath.FromAppDir("judgement.png"));
        }

        public override void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
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
            var posdisp = (ps.Current.Seconds - x.Seconds) * 2.4;
            var displacement = (ps.Current.Seconds - x.Seconds) * 1.2 / RingShowingPeriodByMeasure;  // >= 0
            //int idx = (int)Math.Floor((b.transp.BeatToSeconds(JustDisplacement) - x.Seconds) * 30) + 1;
            int idx = 0;
            var xpos = 40f + ObjectPosX[(x.BMSChannel + (false ? 0 : 1) * 36) % 72] * ttt * 0.8f;
            var xpos2 = 40f + 180 * ttt * 0.8f;

            if (x.Broken)
            {
                // キー押し下しがあった
                idx = (int)Math.Floor((ps.Current.Seconds - x.BrokeAt) * 60) + 1;
            }

            if (idx <= 0)
            {
                // キー押し下しがなかった場合の処理

                idx = (int)Math.Floor((ps.Current.Seconds - x.Seconds) * 30) + 1;
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
                int score = (int)x.Judge - 1;  // 0,1,2,3. 3:pg;
                if (score < 0) score = 0;

                rt.DrawBitmapSrc(judgement,
                    xpos - 64f + 16f, -((float)x.Measure + 1) % RingShowingPeriodByMeasure / RingShowingPeriodByMeasure * 360 + 420f - 32f + 39f,
                    idx / 2 % 2 * 128, (3 - score) * 64,
                    128, 64,
                    1.0f, 1.0f);

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
                var displacement =ps.Current.Seconds - ps.LastKeyEvent.seconds;  // >= 0

                if (((int)(displacement * 30)) < 16)
                {
                    rt.DrawBitmapSrc(bar_white,
                        xpos2 - 256f + 16f, -(((float)b.transp.SecondsToBeat(ps.LastKeyEvent.seconds) / 4 + 0.3f) % RingShowingPeriodByMeasure - 0.3f) / RingShowingPeriodByMeasure * 360 + 420f - 8f,
                        //0, 0 + 12, 
                        0, ((int)(displacement * 30)) * 16,
                        512, 16,
                        0.5f);
                }
                if (((int)(displacement * 30)) < 16)
                {
                    rt.DrawBitmapSrc(bar_white,
                        xpos2 - 256f + 16f, -(((float)b.transp.SecondsToBeat(ps.LastKeyEvent.seconds) / 4 + 0.3f) % RingShowingPeriodByMeasure - 0.3f + RingShowingPeriodByMeasure) / RingShowingPeriodByMeasure * 360 + 420f - 8f,
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
