using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia.RenderTests;

/// <summary>
/// An offscreen GL surface that renders into an FBO and reads the pixels back
/// into a writeable bitmap at the end of each rendering session.
/// </summary>
internal class FboReadbackGlPlatformSurface : IGlPlatformSurface
{
    private readonly IWriteableBitmapImpl _bitmap;
    private readonly double _scaling;

    public FboReadbackGlPlatformSurface(IWriteableBitmapImpl bitmap, double scaling)
    {
        _bitmap = bitmap;
        _scaling = scaling;
    }

    public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context) =>
        new RenderTarget(context, _bitmap, _scaling);

    private class RenderTarget : IGlPlatformSurfaceRenderTarget
    {
        private static readonly bool[] s_trueFalse = { true, false };
        private readonly IGlContext _context;
        private readonly IWriteableBitmapImpl _bitmap;
        private readonly double _scaling;
        private int _fbo;
        private int _color;
        private int _depthStencil;
        private PixelSize _size;

        public RenderTarget(IGlContext context, IWriteableBitmapImpl bitmap, double scaling)
        {
            _context = context;
            _bitmap = bitmap;
            _scaling = scaling;
        }

        public void Dispose()
        {
            if (_fbo == 0 || _context.IsLost)
                return;
            using (_context.MakeCurrent())
                DeleteFbo(_context.GlInterface);
        }

        private void DeleteFbo(GlInterface gl)
        {
            if (_fbo == 0)
                return;
            gl.DeleteFramebuffer(_fbo);
            gl.DeleteRenderbuffer(_color);
            if (_depthStencil != 0)
                gl.DeleteRenderbuffer(_depthStencil);
            _fbo = _color = _depthStencil = 0;
        }

        private void EnsureFbo(GlInterface gl, PixelSize size)
        {
            if (_fbo != 0 && size == _size)
                return;
            DeleteFbo(gl);

            _fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);

            _color = gl.GenRenderbuffer();
            gl.BindRenderbuffer(GL_RENDERBUFFER, _color);
            gl.RenderbufferStorage(GL_RENDERBUFFER, GL_RGBA8, size.Width, size.Height);
            gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_RENDERBUFFER, _color);

            var success = false;
            foreach (var useStencil8 in s_trueFalse)
            {
                _depthStencil = gl.GenRenderbuffer();
                gl.BindRenderbuffer(GL_RENDERBUFFER, _depthStencil);

                if (useStencil8)
                {
                    gl.RenderbufferStorage(GL_RENDERBUFFER, GL_STENCIL_INDEX8, size.Width, size.Height);
                    gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
                }
                else
                {
                    gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, size.Width, size.Height);
                    gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
                    gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
                }

                if (gl.CheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE)
                {
                    success = true;
                    break;
                }

                gl.DeleteRenderbuffer(_depthStencil);
                _depthStencil = 0;
            }

            if (!success)
            {
                DeleteFbo(gl);
                throw new OpenGlException("Unable to create FBO with stencil");
            }

            _size = size;
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            var restoreContext = _context.MakeCurrent();
            var success = false;
            try
            {
                var gl = _context.GlInterface;
                var size = _bitmap.PixelSize;
                EnsureFbo(gl, size);
                gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);
                gl.Viewport(0, 0, size.Width, size.Height);
                success = true;
                return new Session(this, restoreContext);
            }
            finally
            {
                if (!success)
                    restoreContext.Dispose();
            }
        }

        private void ReadPixels()
        {
            var gl = _context.GlInterface;
            gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);
            using (var fb = _bitmap.Lock())
            {
                var tightStride = _size.Width * 4;
                if (fb.RowBytes == tightStride)
                    gl.ReadPixels(0, 0, _size.Width, _size.Height, GL_RGBA, GL_UNSIGNED_BYTE, fb.Address);
                else
                {
                    var temp = Marshal.AllocHGlobal(tightStride * _size.Height);
                    try
                    {
                        gl.ReadPixels(0, 0, _size.Width, _size.Height, GL_RGBA, GL_UNSIGNED_BYTE, temp);
                        for (var y = 0; y < _size.Height; y++)
                            unsafe
                            {
                                Buffer.MemoryCopy(
                                    (byte*)temp + y * tightStride,
                                    (byte*)fb.Address + y * fb.RowBytes,
                                    fb.RowBytes, tightStride);
                            }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(temp);
                    }
                }
            }
        }

        private class Session : IGlPlatformSurfaceRenderingSession
        {
            private readonly RenderTarget _target;
            private readonly IDisposable _restoreContext;

            public Session(RenderTarget target, IDisposable restoreContext)
            {
                _target = target;
                _restoreContext = restoreContext;
            }

            public IGlContext Context => _target._context;
            public PixelSize Size => _target._size;
            public double Scaling => _target._scaling;

            // The FBO is never presented anywhere, so we get to pick the orientation that
            // makes glReadPixels return rows in top-down order.
            public bool IsYFlipped => true;

            public void Dispose()
            {
                try
                {
                    _target._context.GlInterface.Flush();
                    _target.ReadPixels();
                }
                finally
                {
                    _restoreContext.Dispose();
                }
            }
        }
    }
}
