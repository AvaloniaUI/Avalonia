using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia
{
    class BitmapImpl : PerspexHandleHolder, IRenderTargetBitmapImpl
    {
        private int width;
        private int height;

        public void Save(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLower();
            var type = MethodTable.SkiaImageType.Png;
            if(ext=="gif")
                type = MethodTable.SkiaImageType.Gif;
            if(ext=="jpeg" || ext =="jpg")
                type = MethodTable.SkiaImageType.Jpeg;
            var skdata = MethodTable.Instance.SaveImage(Handle, type, 100);
            var size = MethodTable.Instance.GetSkDataSize(skdata);
            var buffer = new byte[size];
            MethodTable.Instance.ReadSkData(skdata, buffer, size);
            File.WriteAllBytes(fileName, buffer);
        }

        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }

        protected override void Delete(IntPtr handle)
        {
            MethodTable.Instance.DisposeImage(handle);
        }
        
        public DrawingContext CreateDrawingContext()
        {
            return
                new DrawingContext(
                    new DrawingContextImpl(MethodTable.Instance.RenderTargetCreateRenderingContext(Handle)));
        }

        public BitmapImpl(IntPtr handle, int width, int height) : base(handle)
        {
            PixelHeight = height;
            PixelWidth = width;
        }

        public BitmapImpl(int width, int height)
            : this(MethodTable.Instance.CreateRenderTargetBitmap(width, height), width, height)
        {
        }
    }
}
