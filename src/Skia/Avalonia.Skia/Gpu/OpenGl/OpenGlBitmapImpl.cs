using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    class GlOpenGlBitmapImpl : IOpenGlBitmapImpl, IDrawableBitmapImpl
    {
        public IGlContext Context { get; }
        private readonly object _lock = new object();
        private IGlPresentableOpenGlSurface _surface;

        public GlOpenGlBitmapImpl(IGlContext context, PixelSize pixelSize, Vector dpi)
        {
            Context = context;
            PixelSize = pixelSize;
            Dpi = dpi;
        }

        public Vector Dpi { get; }
        public PixelSize PixelSize { get; }
        public int Version { get; private set; }
        public void Save(string fileName) => throw new NotSupportedException();

        public void Save(Stream stream) => throw new NotSupportedException();

        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
            {
                if (_surface == null)
                    return;
                using (var texture = _surface.LockForPresentation())
                {
                    // Try with FBO first if it's available
                    if (texture.Fbo != 0)
                    {
                        var target = new GRBackendRenderTarget(PixelSize.Width, PixelSize.Height, 0, 8,
                            new GRGlFramebufferInfo((uint)texture.Fbo, SKColorType.Rgba8888.ToGlSizedFormat()));
                        using var surface = SKSurface.Create(context.GrContext, target,
                            GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
                        using (var snapshot = surface.Snapshot())
                        {
                            context.Canvas.DrawImage(snapshot, sourceRect, destRect, paint);
                            context.Canvas.Flush();
                        }
                        return;
                    }
                    
                    using (var backendTexture = new GRBackendTexture(
                        PixelSize.Width, PixelSize.Height, false,
                        new GRGlTextureInfo(
                            GlConsts.GL_TEXTURE_2D, (uint)texture.TextureId,
                            (uint)texture.InternalFormat)))
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
        }

        public IOpenGlBitmapAttachment CreateFramebufferAttachment(IGlContext context, Action presentCallback)
        {
            if (!SupportsContext(context))
                throw new OpenGlException("Context is not supported for texture sharing");
            if(Context.IsSharedWith(context))
                return new ContextSharedOpenGlBitmapAttachment(this, context, presentCallback);
            return new OSSharedOpenGlBitmapAttachment(this, (IGlContextWithOSTextureSharing) context, presentCallback);
        }

        public bool SupportsContext(IGlContext context) =>
            Context.IsSharedWith(context)
            || (Context is IGlContextWithOSTextureSharing osShared &&
                osShared.AreOSTextureSharingCompatible(context));

        public void Dispose()
        {

        }

        internal void Present(IGlPresentableOpenGlSurface surface)
        {
            lock (_lock)
            {
                _surface = surface;
            }
        }
    }

    interface IGlPresentableOpenGlSurface : IDisposable
    {
        ILockedPresentableOpenGlSurface LockForPresentation();
    }

    interface ILockedPresentableOpenGlSurface : IDisposable
    {
        int TextureId { get; }
        int InternalFormat { get; }
        int Fbo { get; }
    }

    class LockedGlPresentableOpenGlSurface : ILockedPresentableOpenGlSurface
    {
        private Action _disposeCb;

        public LockedGlPresentableOpenGlSurface(int textureId, int internalFormat, int fbo, Action disposeCb)
        {
            _disposeCb = disposeCb;
            TextureId = textureId;
            Fbo = fbo;
            InternalFormat = internalFormat;
        }

        public void Dispose()
        {
            _disposeCb?.Invoke();
            _disposeCb = null;
        }


        public int TextureId { get; }
        public int InternalFormat { get; }
        public int Fbo { get; }
    }
}
