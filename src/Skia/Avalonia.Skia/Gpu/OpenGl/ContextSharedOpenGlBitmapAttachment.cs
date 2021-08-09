using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.Utilities;

namespace Avalonia.Skia
{
    class ContextSharedOpenGlBitmapAttachment : IOpenGlBitmapAttachment, IGlPresentableOpenGlSurface
    {
        private readonly GlOpenGlBitmapImpl _bitmap;
        private readonly IGlContext _context;
        private readonly Action _presentCallback;
        private readonly int _fbo;
        private readonly int _texture;
        private readonly int _frontBuffer;
        private bool _disposed;
        private readonly DisposableLock _lock = new DisposableLock();

        public ContextSharedOpenGlBitmapAttachment(GlOpenGlBitmapImpl bitmap, IGlContext context, Action presentCallback)
        {
            _bitmap = bitmap;
            _context = context;
            _presentCallback = presentCallback;
            using (_context.EnsureCurrent())
            {
                var glVersion = _context.Version;
                InternalFormat = glVersion.Type == GlProfileType.OpenGLES ? GlConsts.GL_RGBA : GlConsts.GL_RGBA8;
                
                _context.GlInterface.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out _fbo);
                if (_fbo == 0)
                    throw new OpenGlException("Current FBO is 0");

                {
                    var gl = _context.GlInterface;
                    
                    var textures = new int[2];
                    gl.GenTextures(2, textures);
                    _texture = textures[0];
                    _frontBuffer = textures[1];
                    
                    gl.GetIntegerv(GlConsts.GL_TEXTURE_BINDING_2D, out var oldTexture);
                    foreach (var t in textures)
                    {
                        gl.BindTexture(GlConsts.GL_TEXTURE_2D, t);
                        gl.TexImage2D(GlConsts.GL_TEXTURE_2D, 0,
                            InternalFormat,
                            _bitmap.PixelSize.Width, _bitmap.PixelSize.Height,
                            0, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_BYTE, IntPtr.Zero);

                        gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MAG_FILTER, GlConsts.GL_NEAREST);
                        gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MIN_FILTER, GlConsts.GL_NEAREST);
                    }

                    gl.FramebufferTexture2D(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_COLOR_ATTACHMENT0, GlConsts.GL_TEXTURE_2D, _texture, 0);
                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, oldTexture);
                    
                }
            }
        }

        public void Present()
        {
            using (_context.MakeCurrent())
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ContextSharedOpenGlBitmapAttachment));
                
                var gl = _context.GlInterface;
               
                gl.Finish();
                using (_lock.Lock())
                {
                    gl.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out var oldFbo);
                    gl.GetIntegerv(GlConsts.GL_TEXTURE_BINDING_2D, out var oldTexture);
                    gl.GetIntegerv(GlConsts.GL_ACTIVE_TEXTURE, out var oldActive);
                    
                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _fbo);
                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, _frontBuffer);
                    gl.ActiveTexture(GlConsts.GL_TEXTURE0);

                    gl.CopyTexSubImage2D(GlConsts.GL_TEXTURE_2D, 0, 0, 0, 0, 0, _bitmap.PixelSize.Width,
                        _bitmap.PixelSize.Height);

                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, oldFbo);
                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, oldTexture);
                    gl.ActiveTexture(oldActive);
                    
                    gl.Finish();
                }
            }
            
            _bitmap.Present(this);
            _presentCallback();
        }

        public void Dispose()
        {
            var gl = _context.GlInterface;
            _bitmap.Present(null);
            
            if(_disposed)
                return;
            using (_context.MakeCurrent())
            using (_lock.Lock())
            {
                if(_disposed)
                    return;
                _disposed = true;
                gl.DeleteTextures(2, new[] { _texture, _frontBuffer });
            }
        }

        public int InternalFormat { get; }

        public ILockedPresentableOpenGlSurface LockForPresentation() => new LockedGlPresentableOpenGlSurface(_frontBuffer,
            InternalFormat, 0, _lock.Lock().Dispose);
    }
}
