using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    class BitmapImpl : IRenderTargetBitmapImpl
    {
        public SKBitmap Bitmap { get; private set; }

        public BitmapImpl(SKBitmap bm)
        {
            Bitmap = bm;
            PixelHeight = bm.Height;
            PixelWidth = bm.Width;
        }

        public BitmapImpl(int width, int height)
        {
            PixelHeight = height;
            PixelWidth = width;
            var colorType = SKImageInfo.PlatformColorType;
            var runtime = AvaloniaLocator.Current?.GetService<IRuntimePlatform>()?.GetRuntimeInfo();
            if (runtime?.IsDesktop == true && runtime?.OperatingSystem == OperatingSystemType.Linux)
                colorType = SKColorType.Bgra8888;
            Bitmap = new SKBitmap(width, height, colorType, SKAlphaType.Premul);
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }

        public void Save(string fileName)
        {
#if DESKTOP
            IntPtr length;
            using (var sdb = new System.Drawing.Bitmap(PixelWidth, PixelHeight, Bitmap.RowBytes,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb, Bitmap.GetPixels(out length)))
                sdb.Save(fileName);
#else
            //SkiaSharp doesn't expose image encoders yet
#endif
        }

        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }

        class BitmapDrawingContext : DrawingContextImpl
        {
            private readonly SKSurface _surface;

            public BitmapDrawingContext(SKBitmap bitmap) : this(CreateSurface(bitmap))
            {
                
            }

            private static SKSurface CreateSurface(SKBitmap bitmap)
            {
                IntPtr length;
                var rv =  SKSurface.Create(bitmap.Info, bitmap.GetPixels(out length), bitmap.RowBytes);
                if (rv == null)
                    throw new Exception("Unable to create Skia surface");
                return rv;
            }

            public BitmapDrawingContext(SKSurface surface) : base(surface.Canvas)
            {
                _surface = surface;
            }

            public override void Dispose()
            {
                base.Dispose();
                _surface.Dispose();
            }
        }

        public DrawingContext CreateDrawingContext()
        {

            return new DrawingContext(new BitmapDrawingContext(Bitmap));
        }

        public void Save(Stream stream)
        {
            IntPtr length;
            using (var image = SKImage.FromPixels(Bitmap.Info, Bitmap.GetPixels(out length), Bitmap.RowBytes))
            using (var data = image.Encode())
            {
                data.SaveTo(stream);
            }
        }
    }
}
