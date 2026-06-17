using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Win32.DirectX
{
    internal class DxgiSwapchainWindow : EglGlPlatformSurfaceBase, ICompositionEffectsSurface, IDisposable
    {
        private DxgiConnection _connection;
        private EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _window;
        private DxgiRenderTarget? _renderTarget;

        public DxgiSwapchainWindow(DxgiConnection connection, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo window)
        {
            _connection = connection;
            _window = window;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            _renderTarget?.Dispose();

            var eglContext = (EglContext)context;
            using (eglContext.EnsureCurrent())
            {
                _renderTarget = new DxgiRenderTarget(_window, eglContext, _connection, _windowTransparencyLevel);
            }

            return _renderTarget;
        }

        public bool IsBlurSupported(BlurEffect effect)
            => effect == BlurEffect.None;

        public void SetBlur(BlurEffect enable)
        {
            // do nothing
        }

        public void SetTransparencyLevel(WindowTransparencyLevel transparencyLevel)
        {
            _windowTransparencyLevel = transparencyLevel;
            _renderTarget?.SetTransparencyLevel(transparencyLevel);
        }

        private WindowTransparencyLevel _windowTransparencyLevel;

        public void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
        }
    }
}
