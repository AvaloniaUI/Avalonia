using Avalonia.Controls;
using Avalonia.OpenGL.Egl;

namespace Avalonia.Win32;

internal interface ICompositorConnection
{
    Win32CompositionMode CompositionMode { get; }

    bool TransparencySupported { get; }
    bool AcrylicSupported { get; }
    bool MicaSupported { get; }

    object CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info);
}
