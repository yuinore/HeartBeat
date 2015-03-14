using HatoBMSLib;
using HatoDraw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoPainter
{
    public abstract class Skin
    {
        public float RingShowingPeriodByMeasure = 2.0f;

        public double BombDuration = 2.0;

        public abstract void Load(RenderTarget rt, BMSStruct b);

        // あと、BGAな
        public abstract void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps);

        public abstract void DrawKeyFlash(RenderTarget rt, BMSStruct b, PlayingState ps, KeyEvent obj);

        public abstract void DrawNote(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj);

        public abstract void DrawBarLine(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj);

        public abstract void DrawFront(RenderTarget rt, BMSStruct b, PlayingState ps);
    }
}
