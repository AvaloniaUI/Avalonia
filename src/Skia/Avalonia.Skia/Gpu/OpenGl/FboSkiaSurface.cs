using System;
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class FboSkiaSurface : ISkiaSurface
    {
        private readonly GRContext _grContext;
        private readonly IGlContext _glContext;
        private readonly PixelSize _pixelSize;
        private uint _fbo;
        private uint _depthStencil;
        private uint _texture;

        private static readonly bool[] TrueFalse = new[] { true, false };
        public unsafe FboSkiaSurface(GRContext grContext, IGlContext glContext, PixelSize pixelSize, GRSurfaceOrigin surfaceOrigin)
        {
            _grContext = grContext;
            _glContext = glContext;
            _pixelSize = pixelSize;
            var internalFormat = glContext.Version.Type == GlProfileType.OpenGLES ?
                GLEnum.Rgba :
                GLEnum.Rgba8;
            var gl = glContext.GL;
            
            // Save old bindings
            gl.GetInteger(GLEnum.FramebufferBinding, out var oldFbo);
            gl.GetInteger(GLEnum.RenderbufferBinding, out var oldRenderbuffer);
            gl.GetInteger(GLEnum.TextureBinding2D, out var oldTexture);

            // Generate FBO
            _fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // Create a texture to render into
            _texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, _texture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)internalFormat, (uint)pixelSize.Width,
                (uint)pixelSize.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _texture, 0);

            var success = false;
            foreach (var useStencil8 in TrueFalse)
            {
                _depthStencil = gl.GenRenderbuffer();
                gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthStencil);

                if (useStencil8)
                {
                    gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.StencilIndex8, (uint)pixelSize.Width, (uint)pixelSize.Height);
                    gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, _depthStencil);
                }
                else
                {
                    gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)pixelSize.Width, (uint)pixelSize.Height);
                    gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthStencil);
                    gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, _depthStencil);
                }

                var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (status == GLEnum.FramebufferComplete)
                {
                    success = true;
                    break;
                }
                else
                {
                    gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, (uint)oldRenderbuffer);
                    gl.DeleteRenderbuffer(_depthStencil);
                }
            }
            
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, (uint)oldRenderbuffer);
            gl.BindTexture(TextureTarget.Texture2D, (uint)oldTexture);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)oldFbo);

            if (!success)
            {
                gl.DeleteFramebuffer(_fbo);
                gl.DeleteTexture(_texture);
                throw new OpenGlException("Unable to create FBO with stencil");
            }

            var target = new GRBackendRenderTarget(pixelSize.Width, pixelSize.Height, 0, 8,
                new GRGlFramebufferInfo((uint)_fbo, SKColorType.Rgba8888.ToGlSizedFormat()));
            Surface = SKSurface.Create(_grContext, target,
                surfaceOrigin, SKColorType.Rgba8888);
            CanBlit = gl.Context.GetProcAddress("glBlitFramebuffer") != 0;
        }
        
        public void Dispose()
        {
            using (_glContext.EnsureCurrent())
            {
                Surface?.Dispose();
                Surface = null;
                var gl = _glContext.GL;
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
            
            var gl = _glContext.GL;
            gl.GetInteger(GetPName.ReadFramebufferBinding, out var oldRead);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
            gl.BlitFramebuffer(0, 0, _pixelSize.Width, _pixelSize.Height, 0, 0, _pixelSize.Width, _pixelSize.Height,
                (uint) GLEnum.ColorBufferBit, BlitFramebufferFilter.Linear);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)oldRead);
        }
    }
}
