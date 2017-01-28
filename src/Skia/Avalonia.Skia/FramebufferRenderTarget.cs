using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class FramebufferRenderTarget : IRenderTarget
    {
        private readonly IFramebufferPlatformSurface _surface;

        public FramebufferRenderTarget(IFramebufferPlatformSurface surface)
        {
            _surface = surface;
        }

        public void Dispose()
        {
            //Nothing to do here, since we don't own framebuffer
        }

        class FramebufferDrawingContextImpl : DrawingContextImpl
        {
            private readonly SKCanvas _canvas;
            private readonly SKSurface _surface;
            private readonly ILockedFramebuffer _framebuffer;

            public FramebufferDrawingContextImpl(SKCanvas canvas, SKSurface surface, ILockedFramebuffer framebuffer) : base(canvas)
            {
                _canvas = canvas;
                _surface = surface;
                _framebuffer = framebuffer;
            }

            public override void Dispose()
            {
                _canvas.Dispose();
                _surface.Dispose();
                _framebuffer.Dispose();
                base.Dispose();
            }
        }

        SKColorType TranslatePixelFormat(PixelFormat fmt)
        {
            if(fmt == PixelFormat.Rgb565)
                return SKColorType.Rgb565;
            if(fmt == PixelFormat.Bgra8888)
                return SKColorType.Bgra8888;
            if (fmt == PixelFormat.Rgba8888)
                return SKColorType.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public DrawingContext CreateDrawingContext()
        {
            var fb = _surface.Lock();
            
            SKImageInfo nfo = new SKImageInfo(fb.Width, fb.Height, TranslatePixelFormat(fb.Format),
                SKAlphaType.Opaque);
            var surface = SKSurface.Create(nfo, fb.Address, fb.RowBytes);
            if (surface == null)
                throw new Exception("Unable to create a surface for pixel format " + fb.Format);
            var canvas = surface.Canvas;
            canvas.RestoreToCount(0);
            canvas.Save();
            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();
            
            return new DrawingContext(new FramebufferDrawingContextImpl(canvas, surface, fb),
                Matrix.CreateScale(fb.Dpi.Width / 96, fb.Dpi.Height / 96));
        }
    }
}
