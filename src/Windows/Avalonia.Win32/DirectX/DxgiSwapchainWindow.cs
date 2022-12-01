using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Win32.DxgiSwapchain
{
    public class DxgiSwapchainWindow : EglGlPlatformSurfaceBase
    {
        private DxgiConnection _connection;
        private EglPlatformOpenGlInterface _egl;
        private IEglWindowGlPlatformSurfaceInfo _window;

        public DxgiSwapchainWindow(DxgiConnection connection, IEglWindowGlPlatformSurfaceInfo window)
        {
            _connection = connection;
            _window = window;
            _egl = connection.Egl;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            using (_egl.PrimaryContext.EnsureCurrent())
            {
                return new DxgiRenderTarget(_window, _egl, _connection);
            }
        }
    }
}
