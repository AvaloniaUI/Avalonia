using System;
using Avalonia.OpenGL.Egl;

namespace Avalonia.Wayland.Egl
{
    internal class WlEglSurfaceInfo : EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
    {
        public WlEglSurfaceInfo(WlWindow wlWindow)
        {
            WlWindow = wlWindow;
        }

        public WlWindow WlWindow { get; }

        public IntPtr Handle { get; set; }

        public PixelSize Size => WlWindow.AppliedState.Size;

        public double Scaling => WlWindow.RenderScaling;
    }
}
