using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia
{
    public partial class PlatformRenderInterface : IPlatformRenderInterface, IRendererFactory
    {
        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return CreateRenderTargetBitmap(width, height, 96, 96);
        }

        public IFormattedTextImpl CreateFormattedText(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight, TextWrapping wrapping, Size constraint)
        {
            return new FormattedTextImpl(text, fontFamilyName, fontSize, fontStyle, textAlignment, fontWeight, wrapping, constraint);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(System.IO.Stream stream)
        {
            using (var s = new SKManagedStream(stream))
            {
                var bitmap = SKBitmap.Decode(s);
                if (bitmap != null)
                {
                    return new BitmapImpl(bitmap);
                }
                else
                {
                    throw new ArgumentException("Unable to load bitmap from provided data");
                }
            }
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return LoadBitmap(stream);
            }
        }

        public IRenderer CreateRenderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            return new DeferredRenderer(root, renderLoop);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(
            int width,
            int height,
            double dpiX,
            double dpiY)
        {
            if (width < 1)
                throw new ArgumentException("Width can't be less than 1", nameof(width));
            if (height < 1)
                throw new ArgumentException("Height can't be less than 1", nameof(height));

            return new BitmapImpl(width, height);
        }

        public virtual IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            var fb = surfaces?.OfType<IFramebufferPlatformSurface>().FirstOrDefault();
            if (fb == null)
                throw new Exception("Skia backend currently only supports framebuffer render target");
            return new FramebufferRenderTarget(fb);
        }
    }
}
