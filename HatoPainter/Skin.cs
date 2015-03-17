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
        // TODO:このメンバをどうにかする
        public float RingShowingPeriodByMeasure = 2.0f;

        public double BaseHiSpeed = 1.0;
        public double UserHiSpeed = 1.0;  // BPMに依存しない値。Secondsに掛けて用いる。
        public double HiSpeed  // BPMに依存する値。Beatsに掛けて用いる。
        {
            get
            {
                return BaseHiSpeed * UserHiSpeed;
            }
        }

        /// <summary>
        /// キー入力からボム表示終了までの最大時間
        /// </summary>
        public abstract double BombDuration { get; }

        public abstract double EyesightDisplacementBefore { get; }
        public abstract double EyesightDisplacementAfter { get; }

        public abstract void Load(RenderTarget rt, BMSStruct b);

        // あと、BGAな
        public abstract void DrawBack(RenderTarget rt, BMSStruct b, PlayingState ps);

        public abstract void DrawKeyFlash(RenderTarget rt, BMSStruct b, PlayingState ps, KeyEvent obj);

        public abstract void DrawNote(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj);

        public abstract void DrawBarLine(RenderTarget rt, BMSStruct b, PlayingState ps, BMObject obj);

        public abstract void DrawFront(RenderTarget rt, BMSStruct b, PlayingState ps);
    }
}
