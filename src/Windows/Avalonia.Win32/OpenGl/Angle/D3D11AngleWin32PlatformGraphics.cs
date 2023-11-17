using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;

namespace Avalonia.Win32.OpenGl.Angle;

internal sealed class D3D11AngleWin32PlatformGraphics : IPlatformGraphics, IPlatformGraphicsOpenGlContextFactory
{
    private readonly Win32AngleEglInterface _egl;
    private AngleWin32EglDisplay? _initialDisplay;

    public D3D11AngleWin32PlatformGraphics(Win32AngleEglInterface egl, AngleWin32EglDisplay? initialDisplay)
    {
        _egl = egl;
        _initialDisplay = initialDisplay;
    }

    public bool UsesSharedContext
        => false;

    public IPlatformGraphicsContext CreateContext()
    {
        var display = Interlocked.Exchange(ref _initialDisplay, null);
        if (display is { IsLost: true })
            display = null;

        display ??= AngleWin32EglDisplay.CreateD3D11Display(_egl);
        return CreateContextForDisplay(display);
    }

    private static EglContext CreateContextForDisplay(AngleWin32EglDisplay display)
    {
        var success = false;
        try
        {
            var context = display.CreateContext(new EglContextOptions
            {
                DisposeCallback = display.Dispose,
                ExtraFeatures = new Dictionary<Type, Func<EglContext, object>>
                {
                    [typeof(IGlPlatformSurfaceRenderTargetFactory)] = _ => new AngleD3DTextureFeature(),
                    [typeof(IGlContextExternalObjectsFeature)] = context => new AngleExternalObjectsFeature(context)
                }
            });
            success = true;
            return context;
        }
        finally
        {
            if (!success)
                display.Dispose();
        }
    }

    public IGlContext CreateContext(IEnumerable<GlVersion>? versions)
    {
        if (versions is not null && versions.All(v => v.Type != GlProfileType.OpenGLES || v.Major != 3))
            throw new OpenGlException("Unable to create context with requested version");

        return (IGlContext)CreateContext();
    }

    IPlatformGraphicsContext IPlatformGraphics.GetSharedContext()
        => throw new InvalidOperationException();

    public static D3D11AngleWin32PlatformGraphics? TryCreate(Win32AngleEglInterface egl)
    {
        AngleWin32EglDisplay? display = null;
        try
        {
            display = AngleWin32EglDisplay.CreateD3D11Display(egl);
            using var ctx = display.CreateContext(new EglContextOptions());
            ctx.MakeCurrent().Dispose();
        }
        catch (Exception e)
        {
            display?.Dispose();
            Logger.TryGet(LogEventLevel.Error, "OpenGL")
                ?.Log(null, "Unable to initialize ANGLE-based rendering with DirectX11 : {0}", e);
            return null;
        }

        return new D3D11AngleWin32PlatformGraphics(egl, display);
    }
}
