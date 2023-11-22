using Avalonia.Controls;
using Avalonia.OpenGL.Egl;

namespace Avalonia.Win32;

internal interface IWindowsSurfaceFactory
{
    bool RequiresNoRedirectionBitmap { get; }

    object CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info);
}
