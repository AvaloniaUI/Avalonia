using System;
using System.Reactive.Disposables;
using Avalonia.MicroCom;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Win32.DirectX;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Win32.OpenGl
{
    internal class AngleDxgiSharedTexture : IGlOSSharedTexture
    {
        private readonly EglContext _context;
        private ID3D11Device _device;
        private readonly AngleWin32EglDisplay _angle;
        private ID3D11Texture2D _texture2d;
        private IDXGIKeyedMutex _mutex;
        private IntPtr _surface;

        public AngleDxgiSharedTexture(EglContext context, IntPtr handle, int width, int height)
        {
            _context = context;
            Width = width;
            Height = height;
            ;
            var gl = context.GlInterface;

            var temp = new int[1];

            _angle = (AngleWin32EglDisplay)context.Display;
            var egl = _angle.EglInterface;

            var success = false;
            try
            {
                _device = MicroComRuntime.CreateProxyFor<ID3D11Device>(_angle.GetDirect3DDevice(), false);
                _texture2d = _device.OpenSharedResource<ID3D11Texture2D>(handle);
                _mutex = _texture2d.QueryInterfaceOrNull<IDXGIKeyedMutex>();
                using (context.EnsureCurrent())
                {
                    _surface = egl.CreatePbufferFromClientBuffer(_angle.Handle, EGL_D3D_TEXTURE_ANGLE,
                        _texture2d.GetNativeIntPtr(), _angle.Config,
                        new[]
                        {
                            EGL_WIDTH, width, EGL_HEIGHT, height, EGL_TEXTURE_FORMAT, EGL_TEXTURE_RGBA,
                            EGL_TEXTURE_TARGET, EGL_TEXTURE_2D, EGL_TEXTURE_INTERNAL_FORMAT_ANGLE, GL_RGBA,
                            EGL_NONE, EGL_NONE, EGL_NONE
                        });

                    gl.GenTextures(1, temp);
                    TextureId = temp[0];

                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, TextureId);

                    if (!egl.BindTexImage(_angle.Handle, _surface, EGL_BACK_BUFFER))
                        throw new OpenGlException("eglBindTexImage failed:" + egl.GetError());

                    gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MAG_FILTER, GlConsts.GL_NEAREST);
                    gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MIN_FILTER, GlConsts.GL_NEAREST);
                    gl.GenFramebuffers(1, temp);
                    Fbo = temp[0];
                    gl.BindFramebuffer(GL_FRAMEBUFFER, Fbo);
                    gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, TextureId, 0);
                    if (gl.CheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
                        throw new OpenGlException("Unable to configure a FBO with DXGI shared-handle imported texture");
                    success = true;
                }
            }
            finally
            {
                if (!success)
                    Dispose();
            }
        }

        public void Dispose()
        {
            using (_context.EnsureCurrent())
            {
                var gl = _context.GlInterface;
                if (Fbo != 0)
                {
                    gl.DeleteFramebuffers(1, new[] { Fbo });
                    Fbo = 0;
                }

                if (TextureId != 0)
                {
                    gl.DeleteTextures(1, new[] { TextureId });
                    TextureId = 0;
                }

                if (_surface != IntPtr.Zero)
                {
                    _angle.EglInterface.DestroySurface(_angle.Handle, _surface);
                    _surface = IntPtr.Zero;
                }
                
                _mutex?.Dispose();
                _mutex = null;

                _texture2d?.Dispose();
                _texture2d = null;
                _device = null;
            }
        }

        public int TextureId { get; private set; }
        public int Fbo { get; private set; }
        public IDisposable Lock()
        {
            if (_mutex == null)
                return Disposable.Empty;
            _mutex.AcquireSync(0, 2000);
            return Disposable.Create(() => _mutex.ReleaseSync(0));
        }
        public int Width { get; }
        public int Height { get; }
    }
}
