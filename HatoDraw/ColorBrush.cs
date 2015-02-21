using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D = SharpDX.Direct2D1;
using SharpDX;

namespace HatoDraw
{
    public class ColorBrush : IDisposable
    {
        internal D2D.Brush d2dBrush;

        /*internal Brush(D2D.Brush brush)
        {
            this.d2dBrush = brush;
        }*/

        public ColorBrush(RenderTarget renderTarget, uint colorRgb)
        {
            d2dBrush = new D2D.SolidColorBrush(renderTarget.d2dRenderTarget, Color.FromBgra(colorRgb | 0xFF000000u));  // 0xAARRGGBB の順
        }

        public ColorBrush(RenderTarget renderTarget, uint colorRgb, float opacity)
        {
            int opacity2 = (int)Math.Round(opacity * 255);
            if (opacity2 > 255) opacity2 = 255;
            if (opacity2 < 0) opacity2 = 0;
            d2dBrush = new D2D.SolidColorBrush(renderTarget.d2dRenderTarget, Color.FromBgra(colorRgb | ((uint)opacity2 << 24)));  // 0xAARRGGBB の順
        }

        //********* implementation of IDisposable *********//

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

            if (disposing)
            {
                // Free any other managed objects here.
                if (d2dBrush != null)
                {
                    d2dBrush.Dispose();
                    d2dBrush = null;
                }
            }
            else
            {
                try
                {
                    throw new Exception("激おこ @ ColorBrush.Dispose(bool)");
                }
                catch
                {
                }
            }

            // Free any unmanaged objects here.
            disposed = true;
        }
        //********* implementation of IDisposable *********//
    }
}
