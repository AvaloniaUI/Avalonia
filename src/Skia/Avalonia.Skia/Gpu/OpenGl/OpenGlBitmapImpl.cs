using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.Utilities;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    class GlOpenGlBitmapImpl : IOpenGlBitmapImpl, IDrawableBitmapImpl
    {
        private readonly IGlContext _context;
        private readonly object _lock = new object();
        private IGlPresentableOpenGlSurface _surface;

        public GlOpenGlBitmapImpl(IGlContext context, PixelSize pixelSize, Vector dpi)
        {
            _context = context;
            PixelSize = pixelSize;
            Dpi = dpi;
        }

        public Vector Dpi { get; }
        public PixelSize PixelSize { get; }
        public int Version { get; private set; }
        public void Save(string fileName, int? quality = null) => throw new NotSupportedException();

        public void Save(Stream stream, int? quality = null) => throw new NotSupportedException();

        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
            {
                if (_surface == null)
                    return;
                using (_surface.Lock())
                {
                    using (var backendTexture = new GRBackendTexture(PixelSize.Width, PixelSize.Height, false,
                        new GRGlTextureInfo(
                            GlConsts.GL_TEXTURE_2D, (uint)_surface.GetTextureId(),
                            (uint)_surface.InternalFormat)))
                    using (var surface = SKSurface.Create(context.GrContext, backendTexture, GRSurfaceOrigin.BottomLeft,
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
            return new SharedOpenGlBitmapAttachment(this, context, presentCallback);
        }

        public bool SupportsContext(IGlContext context)
        {
            // TODO: negotiated platform surface sharing
            return _context.IsSharedWith(context);
        }

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
        int GetTextureId();
        int InternalFormat { get; }
        IDisposable Lock();
    }

    class SharedOpenGlBitmapAttachment : IOpenGlBitmapAttachment, IGlPresentableOpenGlSurface
    {
        private readonly GlOpenGlBitmapImpl _bitmap;
        private readonly IGlContext _context;
        private readonly Action _presentCallback;
        private readonly int _fbo;
        private readonly int _texture;
        private readonly int _frontBuffer;
        private bool _disposed;
        private readonly DisposableLock _lock = new DisposableLock();

        public unsafe SharedOpenGlBitmapAttachment(GlOpenGlBitmapImpl bitmap, IGlContext context, Action presentCallback)
        {
            _bitmap = bitmap;
            _context = context;
            _presentCallback = presentCallback;
            using (_context.EnsureCurrent())
            {
                var glVersion = _context.Version;
                InternalFormat = glVersion.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;
                
                _context.GlInterface.GetIntegerv(GL_FRAMEBUFFER_BINDING, out _fbo);
                if (_fbo == 0)
                    throw new OpenGlException("Current FBO is 0");

                {
                    var gl = _context.GlInterface;
                    
                    Span<int> textures = stackalloc int[2];
                    fixed (int* ptex = textures)
                        gl.GenTextures(2, ptex);
                    _texture = textures[0];
                    _frontBuffer = textures[1];

                    gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);
                    foreach (var t in textures)
                    {
                        gl.BindTexture(GL_TEXTURE_2D, t);
                        gl.TexImage2D(GL_TEXTURE_2D, 0,
                            InternalFormat,
                            _bitmap.PixelSize.Width, _bitmap.PixelSize.Height,
                            0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);

                        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
                        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
                    }

                    gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _texture, 0);
                    gl.BindTexture(GL_TEXTURE_2D, oldTexture);
                }
            }
        }

        public void Present()
        {
            using (_context.MakeCurrent())
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SharedOpenGlBitmapAttachment));
                
                var gl = _context.GlInterface;
               
                gl.Finish();
                using (Lock())
                {
                    gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFbo);
                    gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);
                    gl.GetIntegerv(GL_ACTIVE_TEXTURE, out var oldActive);
                    
                    gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);
                    gl.ActiveTexture(GL_TEXTURE0);
                    gl.BindTexture(GL_TEXTURE_2D, _frontBuffer);

                    gl.CopyTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, 0, 0, _bitmap.PixelSize.Width,
                        _bitmap.PixelSize.Height);

                    gl.BindFramebuffer(GL_FRAMEBUFFER, oldFbo);
                    gl.ActiveTexture(oldActive);
                    gl.BindTexture(GL_TEXTURE_2D, oldTexture);
                    
                    gl.Finish();
                }
            }
            
            _bitmap.Present(this);
            _presentCallback();
        }

        public unsafe void Dispose()
        {
            var gl = _context.GlInterface;
            _bitmap.Present(null);
            
            if(_disposed)
                return;
            using (_context.MakeCurrent())
            using (Lock())
            {
                if(_disposed)
                    return;
                _disposed = true;
                var ptex = stackalloc[] { _texture, _frontBuffer };
                gl.DeleteTextures(2, ptex);
            }
        }

        int IGlPresentableOpenGlSurface.GetTextureId()
        {
            return _frontBuffer;
        }

        public int InternalFormat { get; }

        public IDisposable Lock() => _lock.Lock();
    }
}
