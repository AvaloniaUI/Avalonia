using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;
using SkiaSharp;

namespace Perspex.Skia
{
    class BitmapImpl : IRenderTargetBitmapImpl
    {
		public SKBitmap Bitmap { get; private set; }

		public BitmapImpl(SKBitmap bm)
		{
			Bitmap = bm;
			PixelHeight = bm.Width;
			PixelWidth = bm.Height;
		}

		public BitmapImpl(int width, int height)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
		}

		public void Save(string fileName)
        {
			// TODO: Implement this for SkiaSharp
			throw new NotImplementedException();

            //var ext = Path.GetExtension(fileName)?.ToLower();
            //var type = MethodTable.SkiaImageType.Png;
            //if(ext=="gif")
            //    type = MethodTable.SkiaImageType.Gif;
            //if(ext=="jpeg" || ext =="jpg")
            //    type = MethodTable.SkiaImageType.Jpeg;
            //var skdata = MethodTable.Instance.SaveImage(Handle, type, 100);
            //var size = MethodTable.Instance.GetSkDataSize(skdata);
            //var buffer = new byte[size];
            //MethodTable.Instance.ReadSkData(skdata, buffer, size);
            //File.WriteAllBytes(fileName, buffer);
        }

        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }
        
        public DrawingContext CreateDrawingContext()
        {
			return
				new DrawingContext(
					new DrawingContextImpl(null));	// MethodTable.Instance.RenderTargetCreateRenderingContext(Handle)));
        }

    }
}
