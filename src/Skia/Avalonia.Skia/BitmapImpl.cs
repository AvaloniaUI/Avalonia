using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia
{
    class BitmapImpl : IRenderTargetBitmapImpl, IWritableBitmapImpl
    {
        private Vector _dpi;

        public SKBitmap Bitmap { get; private set; }

        public BitmapImpl(SKBitmap bm)
        {
            Bitmap = bm;
            PixelHeight = bm.Height;
            PixelWidth = bm.Width;
            _dpi = new Vector(96, 96);
        }

        static void ReleaseProc(IntPtr address, object ctx)
        {
            ((IUnmanagedBlob) ctx).Dispose();
        }

        private static readonly SKBitmapReleaseDelegate ReleaseDelegate = ReleaseProc;
        
        public BitmapImpl(int width, int height, Vector dpi, PixelFormat? fmt = null)
        {
            PixelHeight = height;
            PixelWidth = width;
            _dpi = dpi;
            var colorType = fmt?.ToSkColorType() ?? SKImageInfo.PlatformColorType;
            var runtimePlatform = AvaloniaLocator.Current?.GetService<IRuntimePlatform>();
            var runtime = runtimePlatform?.GetRuntimeInfo();
            if (runtime?.IsDesktop == true && runtime?.OperatingSystem == OperatingSystemType.Linux)
                colorType = SKColorType.Bgra8888;

            if (runtimePlatform != null)
            {
                Bitmap = new SKBitmap();
                var nfo = new SKImageInfo(width, height, colorType, SKAlphaType.Premul);
                var plat = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
                var blob = plat.AllocBlob(nfo.BytesSize);
                Bitmap.InstallPixels(nfo, blob.Address, nfo.RowBytes, null, ReleaseDelegate, blob);
                
            }
            else 
                Bitmap =  new SKBitmap(width, height, colorType, SKAlphaType.Premul);
            Bitmap.Erase(SKColor.Empty);
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }

        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }

        class BitmapDrawingContext : DrawingContextImpl
        {
            private readonly SKSurface _surface;

            public BitmapDrawingContext(SKBitmap bitmap, Vector dpi, IVisualBrushRenderer visualBrushRenderer)
                : this(CreateSurface(bitmap), dpi, visualBrushRenderer)
            {
                CanUseLcdRendering = false;
            }

            private static SKSurface CreateSurface(SKBitmap bitmap)
            {
                IntPtr length;
                var rv =  SKSurface.Create(bitmap.Info, bitmap.GetPixels(out length), bitmap.RowBytes);
                if (rv == null)
                    throw new Exception("Unable to create Skia surface");
                return rv;
            }

            public BitmapDrawingContext(SKSurface surface, Vector dpi, IVisualBrushRenderer visualBrushRenderer)
                : base(surface.Canvas, dpi, visualBrushRenderer)
            {
                _surface = surface;
            }

            public override void Dispose()
            {
                base.Dispose();
                _surface.Dispose();
            }
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new BitmapDrawingContext(Bitmap, _dpi, visualBrushRenderer);
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

        public void Save(string fileName)
        {
            using (var stream = File.Create(fileName))
                Save(stream);
        }

        class BitmapFramebuffer : ILockedFramebuffer
        {
            private SKBitmap _bmp;

            public BitmapFramebuffer(SKBitmap bmp)
            {
                _bmp = bmp;
                _bmp.LockPixels();
            }

            public void Dispose()
            {
                _bmp.UnlockPixels();
                _bmp = null;
            }

            public IntPtr Address => _bmp.GetPixels();
            public int Width => _bmp.Width;
            public int Height => _bmp.Height;
            public int RowBytes => _bmp.RowBytes;
            public Vector Dpi { get; } = new Vector(96, 96);
            public PixelFormat Format => _bmp.ColorType.ToPixelFormat();
        }

        public ILockedFramebuffer Lock() => new BitmapFramebuffer(Bitmap);
    }
}
