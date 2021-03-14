using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.Utilities;
using Silk.NET.OpenGL;
using SkiaSharp;

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
        public void Save(string fileName) => throw new NotSupportedException();

        public void Save(Stream stream) => throw new NotSupportedException();

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
                            (uint)GLEnum.Texture2D, (uint)_surface.GetTextureId(),
                            (uint)_surface.InternalFormat)))
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
        private readonly uint _fbo;
        private readonly uint _texture;
        private readonly uint _frontBuffer;
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
                InternalFormat = glVersion.Type == GlProfileType.OpenGLES ? (int)GLEnum.Rgba : (int)GLEnum.Rgba8;
                
                _context.GL.GetInteger(GLEnum.FramebufferBinding, out var fbo);
                if (fbo == 0)
                    throw new OpenGlException("Current FBO is 0");
                _fbo = (uint)fbo;

                {
                    var gl = _context.GL;
                    
                    var textures = new uint[2];
                    gl.GenTextures(2, textures);
                    _texture = textures[0];
                    _frontBuffer = textures[1];

                    gl.GetInteger(GetPName.TextureBinding2D, out var oldTexture);
                    foreach (var t in textures)
                    {
                        gl.BindTexture(TextureTarget.Texture2D, t);
                        gl.TexImage2D(TextureTarget.Texture2D, 0,
                            (int)InternalFormat,
                            (uint)_bitmap.PixelSize.Width, (uint)_bitmap.PixelSize.Height,
                            0, GLEnum.Rgba, PixelType.UnsignedByte, null);

                        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
                        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
                    }

                    gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _texture, 0);
                    gl.BindTexture(TextureTarget.Texture2D, (uint)oldTexture);
                    
                }
            }
        }

        public void Present()
        {
            using (_context.MakeCurrent())
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SharedOpenGlBitmapAttachment));
                
                var gl = _context.GL;
               
                gl.Finish();
                using (Lock())
                {
                    gl.GetInteger(GLEnum.FramebufferBinding, out var oldFbo);
                    gl.GetInteger(GetPName.TextureBinding2D, out var oldTexture);
                    gl.GetInteger(GetPName.ActiveTexture, out var oldActive);
                    
                    gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
                    gl.BindTexture(TextureTarget.Texture2D, _frontBuffer);
                    gl.ActiveTexture(TextureUnit.Texture0);

                    gl.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, (uint)_bitmap.PixelSize.Width,
                        (uint)_bitmap.PixelSize.Height);

                    gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)oldFbo);
                    gl.BindTexture(TextureTarget.Texture2D, (uint)oldTexture);
                    gl.ActiveTexture((GLEnum)oldActive);
                    
                    gl.Finish();
                }
            }
            
            _bitmap.Present(this);
            _presentCallback();
        }

        public void Dispose()
        {
            var gl = _context.GL;
            _bitmap.Present(null);
            
            if(_disposed)
                return;
            using (_context.MakeCurrent())
            using (Lock())
            {
                if(_disposed)
                    return;
                _disposed = true;
                gl.DeleteTextures(2, new[] { _texture, _frontBuffer });
            }
        }

        int IGlPresentableOpenGlSurface.GetTextureId()
        {
            return (int)_frontBuffer;
        }

        public int InternalFormat { get; }

        public IDisposable Lock() => _lock.Lock();
    }
}
