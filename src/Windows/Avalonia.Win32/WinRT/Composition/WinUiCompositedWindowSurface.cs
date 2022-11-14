using System;
using System.Runtime.InteropServices;
using Avalonia.MicroCom;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Utilities;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition
{
    internal class WinUiCompositedWindowSurface : EglGlPlatformSurfaceBase, IBlurHost, IDisposable
    {
        private readonly WinUICompositorConnection _connection;
        private EglPlatformOpenGlInterface _egl;
        private readonly EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo _info;
        private IRef<WinUICompositedWindow> _window;
        private BlurEffect _blurEffect;

        public WinUiCompositedWindowSurface(WinUICompositorConnection connection, IEglWindowGlPlatformSurfaceInfo info) : base()
        {
            _connection = connection;
            _egl = connection.Egl;
            _info = info;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            using (_egl.PrimaryContext.EnsureCurrent())
            {
                if (_window?.Item == null)
                {
                    _window = RefCountable.Create(_connection.CreateWindow(_info.Handle));
                    _window.Item.SetBlur(_blurEffect);
                }

                return new CompositionRenderTarget(_egl, _window, _info);
            }
        }

        class CompositionRenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly EglPlatformOpenGlInterface _egl;
            private readonly IRef<WinUICompositedWindow> _window;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;

            public CompositionRenderTarget(EglPlatformOpenGlInterface egl,
                IRef<WinUICompositedWindow> window,
                IEglWindowGlPlatformSurfaceInfo info)
                : base(egl)
            {
                _egl = egl;
                _window = window.Clone();
                _info = info;
                _window.Item.ResizeIfNeeded(_info.Size);
            }

            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var contextLock = _egl.PrimaryEglContext.EnsureCurrent();
                IUnknown texture = null;
                EglSurface surface = null;
                IDisposable transaction = null;
                var success = false;
                try
                {
                    if (_window?.Item == null)
                        throw new ObjectDisposedException(GetType().FullName);
                    
                    var size = _info.Size;
                    transaction = _window.Item.BeginTransaction();
                    _window.Item.ResizeIfNeeded(size);
                    texture = _window.Item.BeginDrawToTexture(out var offset);

                    surface = ((AngleWin32EglDisplay) _egl.Display).WrapDirect3D11Texture(_egl,
                        texture.GetNativeIntPtr(),
                        offset.X, offset.Y, size.Width, size.Height);

                    var res = base.BeginDraw(surface, _info, () =>
                    {
                        surface?.Dispose();
                        texture?.Dispose();
                        _window.Item.EndDraw();
                        transaction?.Dispose();
                        contextLock?.Dispose();
                    }, true);
                    success = true;
                    return res;
                }
                finally
                {
                    if (!success)
                    {
                        surface?.Dispose();
                        texture?.Dispose();
                        transaction?.Dispose();
                        contextLock.Dispose();
                    }
                }
            }
        }

        public void SetBlur(BlurEffect blurEffect)
        {
            _blurEffect = blurEffect;
            _window?.Item?.SetBlur(blurEffect);
        }

        public void Dispose()
        {
            using (_egl.PrimaryEglContext.EnsureLocked())
            {
                _window?.Dispose();
                _window = null;
            }
        }
    }
}
