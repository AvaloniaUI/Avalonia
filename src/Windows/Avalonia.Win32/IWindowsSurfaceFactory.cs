using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.OpenGL.Egl;

namespace Avalonia.Win32;

internal interface IWindowsSurfaceFactory
{
    bool RequiresNoRedirectionBitmap { get; }

    IPlatformRenderSurface CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info);
}
