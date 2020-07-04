using System;
using System.IO;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.Skia.Helpers;
using Avalonia.Utilities;
using SkiaSharp;

namespace Avalonia.Skia
{
    class OpenGlTextureBitmapImpl : IOpenGlTextureBitmapImpl, IDrawableBitmapImpl
    {
        private DisposableLock _lock = new DisposableLock();
        private int _textureId;
        private int _internalFormat;

        public void Dispose()
        {
            using (Lock())
            {
                _textureId = 0;
                PixelSize = new PixelSize(1, 1);
                Version++;
            }
        }

        public Vector Dpi { get; private set; } = new Vector(96, 96);
        public PixelSize PixelSize { get; private set; } = new PixelSize(1, 1);
        public int Version { get; private set; } = 0;

        public void Save(string fileName) => throw new System.NotSupportedException();
        public void Save(Stream stream) => throw new System.NotSupportedException();

        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            // For now silently ignore
            if (context.GrContext == null)
                return;

            using (Lock())
            {
                if (_textureId == 0)
                    return;
                using (var backendTexture = new GRBackendTexture(PixelSize.Width, PixelSize.Height, false,
                    new GRGlTextureInfo(
                        GlConsts.GL_TEXTURE_2D, (uint)_textureId,
                        (uint)_internalFormat)))
                using (var surface = SKSurface.Create(context.GrContext, backendTexture, GRSurfaceOrigin.TopLeft,
                    SKColorType.Rgba8888))
                {
                    // Again, silently ignore, if something went wrong it's not our fault
                    if (surface == null)
                        return;

                    using (var snapshot = surface.Snapshot())
                        context.Canvas.DrawImage(snapshot, sourceRect, destRect, paint);
                }
            }
        }

        public IDisposable Lock() => _lock.Lock();

        public void SetBackBuffer(int textureId, int internalFormat, PixelSize pixelSize, double dpiScaling)
        {
            using (_lock.Lock())
            {
                _textureId = textureId;
                _internalFormat = internalFormat;
                PixelSize = pixelSize;
                Dpi = new Vector(96 * dpiScaling, 96 * dpiScaling);
                Version++;
            }
        }

        public void SetDirty()
        {
            using (_lock.Lock())
                Version++;
        }
    }
}
