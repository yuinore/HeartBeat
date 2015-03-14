using HatoBMSLib;
using HatoDraw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoPainter
{
    public class NullSkin : Skin
    {
        public override double BombDuration
        {
            get { return 2.0f; }
        }

        public override void Load(RenderTarget rt, BMSStruct b)
        {
        }

        public override void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
        }

        public override void DrawKeyFlash(RenderTarget rt, BMSStruct b, PlayingState ps, KeyEvent obj)
        {
        }

        public override void DrawNote(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj)
        {
        }

        public override void DrawBarLine(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj)
        {
        }

        public override void DrawFront(RenderTarget rt, BMSStruct b, PlayingState ps)
        {
        }
    }
}
