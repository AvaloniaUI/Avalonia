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

        public PixelSize Size => new((int)Math.Round(WlWindow.AppliedState.Size.Width * WlWindow.RenderScaling, MidpointRounding.AwayFromZero), (int)Math.Round(WlWindow.AppliedState.Size.Height * WlWindow.RenderScaling, MidpointRounding.AwayFromZero));

        public double Scaling => WlWindow.RenderScaling;
    }
}
