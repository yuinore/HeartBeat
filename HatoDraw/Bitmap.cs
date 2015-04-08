using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WIC = SharpDX.WIC;
using SharpDX.IO;
using System.IO;

namespace HatoDraw
{
    public class BitmapData
    {
        internal SharpDX.Direct2D1.Bitmap d2dBitmap;

        public float Width
        {
            get
            {
                return d2dBitmap.Size.Width;
            }
        }

        public float Height
        {
            get
            {
                return d2dBitmap.Size.Height;
            }
        }

        /// <summary>
        /// 指定したRGB色をキーカラーにして画像を読み込みます。
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="filepath"></param>
        /// <param name="keyColorRGB"></param>
        public BitmapData(RenderTarget renderTarget, string filepath, uint keyColorRGB)
        {
            // Load Image To Direct2D via WIC
            // http://english.r2d2rigo.es/2014/08/12/loading-and-drawing-bitmaps-with-direct2d-using-sharpdx/

            WIC.ImagingFactory imagingFactory = new WIC.ImagingFactory();
            NativeFileStream fileStream = new NativeFileStream(filepath, NativeFileMode.Open, NativeFileAccess.Read);

            WIC.BitmapDecoder bitmapDecoder = new WIC.BitmapDecoder(imagingFactory, fileStream, WIC.DecodeOptions.CacheOnDemand);
            WIC.BitmapFrameDecode frame = bitmapDecoder.GetFrame(0);

            WIC.FormatConverter converter = new WIC.FormatConverter(imagingFactory);
            converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPRGBA);

            // ここまでは共通

            uint[] buf = new uint[converter.Size.Width * converter.Size.Height];
            converter.CopyPixels(buf);  // converterオブジェクトから、32bit RGBAに変換された配列を取得する。
            // ↑ここでSharpDXException [HRESULT = 0x80004005] や [HRESULT = 0x80004003] が発生する。
            // FIXME: 多分何かが間違っているんだと思います。

            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa378137%28v=vs.85%29.aspx
            // E_POINTER    Pointer that is not valid   HRESULT = 0x80004003
            // E_FAIL       Unspecified failure         HRESULT = 0x80004005

            uint bgra = ((keyColorRGB & 0xFF0000u) >> 16) | (keyColorRGB & 0x00FF00u) | ((keyColorRGB & 0x0000FFu) << 16);
                
            for (int i = 0; i < buf.Length; i++)
            {
                if ((buf[i] & 0x00FFFFFF) == bgra)  // RGB から XBGR (リトルエンディアン)に変換 (ただしXは0x00)
                {
                    buf[i] = 0x00000000u;
                }
            }
            var newbmp = WIC.Bitmap.New(imagingFactory, converter.Size.Width, converter.Size.Height, converter.PixelFormat, buf);

            WIC.FormatConverter converter2 = new WIC.FormatConverter(imagingFactory);
            converter2.Initialize(newbmp, SharpDX.WIC.PixelFormat.Format32bppPRGBA);

            d2dBitmap = SharpDX.Direct2D1.Bitmap.FromWicBitmap(renderTarget.d2dRenderTarget, converter2);
        }

        /// <summary>
        /// キーイング無しで画像ファイルを読み込みます。
        /// </summary>
        public BitmapData(RenderTarget renderTarget, string filepath)
        {
            // Load Image To Direct2D via WIC
            // http://english.r2d2rigo.es/2014/08/12/loading-and-drawing-bitmaps-with-direct2d-using-sharpdx/

            WIC.ImagingFactory imagingFactory = new WIC.ImagingFactory();
            NativeFileStream fileStream = new NativeFileStream(filepath, NativeFileMode.Open, NativeFileAccess.Read);

            WIC.BitmapDecoder bitmapDecoder = new WIC.BitmapDecoder(imagingFactory, fileStream, WIC.DecodeOptions.CacheOnDemand);
            WIC.BitmapFrameDecode frame = bitmapDecoder.GetFrame(0);

            WIC.FormatConverter converter = new WIC.FormatConverter(imagingFactory);
            converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPRGBA);

            d2dBitmap = SharpDX.Direct2D1.Bitmap.FromWicBitmap(renderTarget.d2dRenderTarget, converter);
        }
    }
}
