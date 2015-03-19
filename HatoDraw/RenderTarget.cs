using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D = SharpDX.Direct2D1;

namespace HatoDraw
{
    public class RenderTarget
    {
        internal D2D.RenderTarget d2dRenderTarget;

        float sr = 1f;//640f / 512f;

        internal RenderTarget(D2D.RenderTarget renderTarget)
        {
            this.d2dRenderTarget = renderTarget;
        }

        public void ClearWhite()
        {
            d2dRenderTarget.Clear(Color.White);
        }

        public void ClearBlack()
        {
            d2dRenderTarget.Clear(Color.Black);
        }

        public void DrawBitmap(BitmapData bitmap, float posX, float posY, float opacity = 1.0f, float scale = 1.0f)
        {
            bool nearest = (scale == 1.0f || scale >= 2.0f); // スケールが1.0のとき、Linearだと綺麗に表示されないからクソ

            //d2dRenderTarget.DrawBitmap(bitmap.d2dBitmap, new RectangleF(posX, posY, 100, 100), 1.0f, D2D.BitmapInterpolationMode.Linear);
            d2dRenderTarget.DrawBitmap(
                bitmap.d2dBitmap,
                new RectangleF(posX * sr, posY * sr, bitmap.d2dBitmap.Size.Width * scale * sr, bitmap.d2dBitmap.Size.Height * scale * sr),
                opacity,
                nearest ? D2D.BitmapInterpolationMode.NearestNeighbor : D2D.BitmapInterpolationMode.Linear);
        }

        public void DrawBitmapSrc(BitmapData bitmap, float posX, float posY,
            float srcX, float srcY, float width, float height,
            float opacity = 1.0f, float scale = 1.0f)
        {
            bool nearest = (scale == 1.0f || scale >= 2.0f); // スケールが1.0のとき、Linearだと綺麗に表示されないからクソ

            //d2dRenderTarget.DrawBitmap(bitmap.d2dBitmap, new RectangleF(posX, posY, 100, 100), 1.0f, D2D.BitmapInterpolationMode.Linear);
            d2dRenderTarget.DrawBitmap(
                bitmap.d2dBitmap,
                new RectangleF(posX * sr, posY * sr, width * scale * sr, height * scale * sr),
                opacity,
                nearest ? D2D.BitmapInterpolationMode.NearestNeighbor : D2D.BitmapInterpolationMode.Linear,
                new RectangleF(srcX, srcY, width, height));
        }

        public void DrawBitmapRect(BitmapData bitmap,
            float dstX, float dstY, float dstW, float dstH,
            float srcX, float srcY, float srcW, float srcH,
            float opacity = 1.0f, bool nearest = true)
        {
            //bool nearest = (scale == 1.0f || scale >= 2.0f); // スケールが1.0のとき、Linearだと綺麗に表示されないからクソ

            //d2dRenderTarget.DrawBitmap(bitmap.d2dBitmap, new RectangleF(posX, posY, 100, 100), 1.0f, D2D.BitmapInterpolationMode.Linear);
            d2dRenderTarget.DrawBitmap(
                bitmap.d2dBitmap,
                new RectangleF(dstX * sr, dstY * sr, dstW * sr, dstH * sr),
                opacity,
                nearest ? D2D.BitmapInterpolationMode.NearestNeighbor : D2D.BitmapInterpolationMode.Linear,
                new RectangleF(srcX, srcY, srcW, srcH));
        }

        public void FillRectangle(float posX, float posY, float width, float height, ColorBrush brush)
        {
            d2dRenderTarget.FillRectangle(new RectangleF(posX * sr, posY * sr, width * sr, height * sr), brush.d2dBrush);
        }

        public void FillRectangle(float posX, float posY, float width, float height, uint colorRgb, float opacity = 1.0f)
        {
            // ColorBrushの解放が面倒な人向け
            using (var b = new ColorBrush(this, colorRgb, opacity))
            {
                d2dRenderTarget.FillRectangle(new RectangleF(posX * sr, posY * sr, width * sr, height * sr), b.d2dBrush);
            }
        }

        public void DrawRectangle(float posX, float posY, float width, float height, ColorBrush brush, float strokewidth)
        {
            d2dRenderTarget.DrawRectangle(new RectangleF(posX * sr, posY * sr, width * sr, height * sr), brush.d2dBrush, strokewidth);
        }

        public void DrawText(BitmapData font, string text, float posX, float posY, float scale = 0.75f)
        {
            int x = 0;
            int y = 0;
            float s = scale;

            foreach (var c in text)
            {
                if (c == '\r')
                {
                    continue;
                }
                else if (c == '\n')
                {
                    x = 0;
                    y += 1;
                    if (y >= 20) break;
                    continue;
                }
                else
                {
                    int charcode = (1 <= c && c <= 31 || 127 <= c) ? 63 : c;

                    DrawBitmapSrc(font, (posX + (x++) * 12) * s, (posY + y * 18) * s, charcode % 16 * 16, charcode / 16 * 16, 16, 16, 0.5f, s);
                }
            }
        }
    }
}
