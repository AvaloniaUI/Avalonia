using System;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL.Egl
{
    public abstract class EglGlPlatformImageSurfaceBase : IGlPlatformSurface
    {
        public abstract IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context);
    }

    public abstract class EglPlatformImageSurfaceRenderTargetBase : IGlPlatformSurfaceRenderTarget
    {
        protected EglContext Context { get; }
        private int _fbo;
        private int _texture;
        private PixelSize _fboSize;

        protected EglPlatformImageSurfaceRenderTargetBase(EglContext context)
        {
            Context = context;
        }

        public virtual void Dispose()
        {
            DestroyFbo();
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (Context.IsLost)
                throw new RenderTargetCorruptedException();

            return BeginDrawCore(sceneInfo);
        }

        public abstract IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo);

        protected IGlPlatformSurfaceRenderingSession BeginDraw(IntPtr eglImage,
            PixelSize size, double scaling, Action onFinishDraw)
        {
            var restoreContext = Context.MakeCurrent(null);
            var success = false;
            try
            {
                var gl = Context.GlInterface;

                EnsureFbo(gl, size);

                gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);
                gl.BindTexture(GL_TEXTURE_2D, _texture);
                gl.EGLImageTargetTexture2DOES(GL_TEXTURE_2D, eglImage);
                gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0,
                    GL_TEXTURE_2D, _texture, 0);

                var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
                if (status != GL_FRAMEBUFFER_COMPLETE)
                    throw new OpenGlException($"Framebuffer incomplete: 0x{status:X}");

                gl.Viewport(0, 0, size.Width, size.Height);

                success = true;
                return new Session(Context, size, scaling, restoreContext, onFinishDraw);
            }
            finally
            {
                if (!success)
                    restoreContext.Dispose();
            }
        }

        private void EnsureFbo(GlInterface gl, PixelSize size)
        {
            if (_fbo != 0 && _fboSize == size)
                return;

            DestroyFboCore(gl);

            _fbo = gl.GenFramebuffer();
            _texture = gl.GenTexture();
            _fboSize = size;

            gl.BindTexture(GL_TEXTURE_2D, _texture);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        }

        private void DestroyFbo()
        {
            if (_fbo == 0)
                return;
            try
            {
                using (Context.MakeCurrent(null))
                    DestroyFboCore(Context.GlInterface);
            }
            catch
            {
                // Context may already be lost
            }
        }

        private void DestroyFboCore(GlInterface gl)
        {
            if (_fbo != 0)
            {
                gl.DeleteFramebuffer(_fbo);
                _fbo = 0;
            }
            if (_texture != 0)
            {
                gl.DeleteTexture(_texture);
                _texture = 0;
            }
            _fboSize = default;
        }

        private class Session : IGlPlatformSurfaceRenderingSession
        {
            private readonly EglContext _context;
            private readonly IDisposable _restoreContext;
            private readonly Action _onFinishDraw;

            public Session(EglContext context, PixelSize size, double scaling,
                IDisposable restoreContext, Action onFinishDraw)
            {
                _context = context;
                Size = size;
                Scaling = scaling;
                _restoreContext = restoreContext;
                _onFinishDraw = onFinishDraw;
            }

            public void Dispose()
            {
                _context.GlInterface.Flush();
                _restoreContext.Dispose();
                _onFinishDraw();
            }

            public IGlContext Context => _context;
            public PixelSize Size { get; }
            public double Scaling { get; }
            public bool IsYFlipped => true;
        }

        public virtual PlatformRenderTargetState State =>
            IsCorrupted ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

        public virtual bool IsCorrupted => Context.IsLost;
    }
}
