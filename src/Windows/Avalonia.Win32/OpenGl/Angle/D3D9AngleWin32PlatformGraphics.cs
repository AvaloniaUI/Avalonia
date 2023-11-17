using System;
using Avalonia.Logging;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;

namespace Avalonia.Win32.OpenGl.Angle;

internal sealed class D3D9AngleWin32PlatformGraphics : IPlatformGraphics
{
    private readonly AngleWin32EglDisplay _sharedDisplay;
    private EglContext? _sharedContext;

    public D3D9AngleWin32PlatformGraphics(AngleWin32EglDisplay sharedDisplay)
        => _sharedDisplay = sharedDisplay;

    public bool UsesSharedContext
        => true;

    public IPlatformGraphicsContext GetSharedContext()
    {
        if (_sharedContext is { IsLost: true })
        {
            _sharedContext.Dispose();
            _sharedContext = null;
        }

        return _sharedContext ??= _sharedDisplay.CreateContext(new EglContextOptions());
    }

    IPlatformGraphicsContext IPlatformGraphics.CreateContext()
        => throw new InvalidOperationException();

    public static D3D9AngleWin32PlatformGraphics? TryCreate(Win32AngleEglInterface egl)
    {
        AngleWin32EglDisplay? sharedDisplay = null;
        try
        {
            sharedDisplay = AngleWin32EglDisplay.CreateD3D9Display(egl);
            using var ctx = sharedDisplay.CreateContext(new EglContextOptions());
            ctx.MakeCurrent().Dispose();
        }
        catch (Exception e)
        {
            sharedDisplay?.Dispose();
            Logger.TryGet(LogEventLevel.Error, "OpenGL")
                ?.Log(null, "Unable to initialize ANGLE-based rendering with DirectX9 : {0}", e);
            return null;
        }

        return new D3D9AngleWin32PlatformGraphics(sharedDisplay);
    }
}
