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
    class SimpleRingSkin : Skin
    {
        private BitmapData bmp;
        private BitmapData bomb;
        private BitmapData bar;
        private BitmapData bar_white;
        private BitmapData judgement;

        int left = 0;
        int right = 0;
        private BitmapData bga_back;
        private BitmapData bga_front;

        public override void Load(RenderTarget rt, BMSStruct b)
        {
            if (b.Stagefile != null && File.Exists(b.ToFullPath(b.Stagefile)))
            {
                bmp = new BitmapData(rt, b.ToFullPath(b.Stagefile));
            }
            
            bomb = new BitmapData(rt, HatoPath.FromAppDir("ring1.png"));
            bar = new BitmapData(rt, HatoPath.FromAppDir("bar1.png"));
            bar_white = new BitmapData(rt, HatoPath.FromAppDir( "bar1_white.png"));

            judgement = new BitmapData(rt, HatoPath.FromAppDir("judgement.png"));
        }

        public override void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
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
        }
    }
}
