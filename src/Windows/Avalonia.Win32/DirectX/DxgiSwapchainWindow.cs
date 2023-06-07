using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Win32.DirectX
{
    internal class DxgiSwapchainWindow : EglGlPlatformSurfaceBase
    {
        private DxgiConnection _connection;
        private EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _window;

        public DxgiSwapchainWindow(DxgiConnection connection, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo window)
        {
            _connection = connection;
            _window = window;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            var eglContext = (EglContext)context;
            using (eglContext.EnsureCurrent())
            {
                return new DxgiRenderTarget(_window, eglContext, _connection);
            }
        }
    }
}
