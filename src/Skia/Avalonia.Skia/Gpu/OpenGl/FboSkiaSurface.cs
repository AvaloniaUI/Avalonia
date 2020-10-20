using System;
using Avalonia.OpenGL;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;
namespace Avalonia.Skia
{
    public class FboSkiaSurface : ISkiaSurface
    {
        private readonly GRContext _grContext;
        private readonly IGlContext _glContext;
        private readonly PixelSize _pixelSize;
        private int _fbo;
        private int _depthStencil;
        private int _texture;

        private static readonly bool[] TrueFalse = new[] { true, false };
        public FboSkiaSurface(GRContext grContext, IGlContext glContext, PixelSize pixelSize, GRSurfaceOrigin surfaceOrigin)
        {
            _grContext = grContext;
            _glContext = glContext;
            _pixelSize = pixelSize;
            var InternalFormat = glContext.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8;
            var gl = glContext.GlInterface;
            
            // Save old bindings
            gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFbo);
            gl.GetIntegerv(GL_RENDERBUFFER_BINDING, out var oldRenderbuffer);
            gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);

            var arr = new int[2];

            // Generate FBO
            gl.GenFramebuffers(1, arr);
            _fbo = arr[0];
            gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);

            // Create a texture to render into
            gl.GenTextures(1, arr);
            _texture = arr[0];
            gl.BindTexture(GL_TEXTURE_2D, _texture);
            gl.TexImage2D(GL_TEXTURE_2D, 0,
                InternalFormat, pixelSize.Width, pixelSize.Height,
                0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _texture, 0);

            var success = false;
            foreach (var useStencil8 in TrueFalse)
            {
                gl.GenRenderbuffers(1, arr);
                _depthStencil = arr[0];
                gl.BindRenderbuffer(GL_RENDERBUFFER, _depthStencil);

                if (useStencil8)
                {
                    gl.RenderbufferStorage(GL_RENDERBUFFER, GL_STENCIL_INDEX8, pixelSize.Width, pixelSize.Height);
                    gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
                }
                else
                {
                    gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, pixelSize.Width, pixelSize.Height);
                    gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
                    gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
                }

                var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
                if (status == GL_FRAMEBUFFER_COMPLETE)
                {
                    success = true;
                    break;
                }
                else
                {
                    gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderbuffer);
                    gl.DeleteRenderbuffers(1, arr);
                }
            }
            
            gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderbuffer);
            gl.BindTexture(GL_TEXTURE_2D, oldTexture);
            gl.BindFramebuffer(GL_FRAMEBUFFER, oldFbo);

            if (!success)
            {
                arr[0] = _fbo;
                gl.DeleteFramebuffers(1, arr);
                arr[0] = _texture;
                gl.DeleteTextures(1, arr);
                throw new OpenGlException("Unable to create FBO with stencil");
            }

            var target = new GRBackendRenderTarget(pixelSize.Width, pixelSize.Height, 0, 8,
                new GRGlFramebufferInfo((uint)_fbo, SKColorType.Rgba8888.ToGlSizedFormat()));
            Surface = SKSurface.Create(_grContext, target,
                surfaceOrigin, SKColorType.Rgba8888);
            CanBlit = gl.BlitFramebuffer != null;
        }
        
        public void Dispose()
        {
            using (_glContext.EnsureCurrent())
            {
                Surface?.Dispose();
                Surface = null;
                var gl = _glContext.GlInterface;
                if (_fbo != 0)
                {
                    gl.DeleteFramebuffers(1, new[] { _fbo });
                    gl.DeleteTextures(1, new[] { _texture });
                    gl.DeleteRenderbuffers(1, new[] { _depthStencil });
                    _fbo = _texture = _depthStencil = 0;
                }
            }
        }

        public SKSurface Surface { get; private set; }
        public bool CanBlit { get; }
        public void Blit(SKCanvas canvas)
        {
            // This should set the render target as the current FBO
            // which is definitely not the best method, but it works
            canvas.Clear();
            canvas.Flush();
            
            var gl = _glContext.GlInterface;
            gl.GetIntegerv(GL_READ_FRAMEBUFFER_BINDING, out var oldRead);
            gl.BindFramebuffer(GL_READ_FRAMEBUFFER, _fbo);
            gl.BlitFramebuffer(0, 0, _pixelSize.Width, _pixelSize.Height, 0, 0, _pixelSize.Width, _pixelSize.Height,
                GL_COLOR_BUFFER_BIT, GL_LINEAR);
            gl.BindFramebuffer(GL_READ_FRAMEBUFFER, oldRead);
        }
    }
}
