using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WIC = SharpDX.WIC;
using SharpDX.IO;

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
