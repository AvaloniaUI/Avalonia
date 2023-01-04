using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;

namespace Avalonia.Win32.OpenGl.Angle;

internal class AngleWin32PlatformGraphics : IPlatformGraphics
{
    private readonly Win32AngleEglInterface _egl;
    private AngleWin32EglDisplay _sharedDisplay;
    private EglContext _sharedContext;
    public bool UsesSharedContext => PlatformApi == AngleOptions.PlatformApi.DirectX9;

    public AngleOptions.PlatformApi PlatformApi { get; } = AngleOptions.PlatformApi.DirectX11;
    public IPlatformGraphicsContext CreateContext()
    {
        if (UsesSharedContext)
            throw new InvalidOperationException();
        
        var display = AngleWin32EglDisplay.CreateD3D11Display(_egl);
        var success = false;
        try
        {
            var rv = display.CreateContext(new EglContextOptions
            {
                DisposeCallback = display.Dispose,
                ExtraFeatures = new Dictionary<Type, object>
                {
                    [typeof(IGlPlatformSurfaceRenderTargetFactory)] = new AngleD3DTextureFeature()
                }
            });
            success = true;
            return rv;
        }
        finally
        {
            if (!success)
                display.Dispose();
        }
    }

    public IPlatformGraphicsContext GetSharedContext()
    {
        if (!UsesSharedContext)
            throw new InvalidOperationException();
        if (_sharedContext == null || _sharedContext.IsLost)
        {
            _sharedContext?.Dispose();
            _sharedContext = null;
            _sharedContext = _sharedDisplay.CreateContext(new EglContextOptions());
        }

        return _sharedContext;
    }

    public AngleWin32PlatformGraphics(Win32AngleEglInterface egl, AngleWin32EglDisplay display) 
        : this(egl, display.PlatformApi)
    {
        _sharedDisplay = display;
    }

    public AngleWin32PlatformGraphics(Win32AngleEglInterface egl, AngleOptions.PlatformApi api)
    {
        _egl = egl;
        PlatformApi = api;
    }


    public static AngleWin32PlatformGraphics TryCreate(AngleOptions options)
    {
         
        
        Win32AngleEglInterface egl;
        try
        {
            egl = new();
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")
                ?.Log(null, "Unable to load ANGLE: {0}", e);
            return null;
        }

        foreach (var api in (options?.AllowedPlatformApis ?? new []
                 {
                     AngleOptions.PlatformApi.DirectX11
                 }).Distinct())
            if (api == AngleOptions.PlatformApi.DirectX11)
            {
                try
                {
                    using var display = AngleWin32EglDisplay.CreateD3D11Display(egl);
                    using var ctx = display.CreateContext(new EglContextOptions());
                    ctx.MakeCurrent().Dispose();
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")
                        ?.Log(null, "Unable to initialize ANGLE-based rendering with DirectX11 : {0}", e);
                    continue;
                }

                return new AngleWin32PlatformGraphics(egl, AngleOptions.PlatformApi.DirectX11);
            }
            else
            {
                AngleWin32EglDisplay sharedDisplay = null;
                try
                {
                    sharedDisplay = AngleWin32EglDisplay.CreateD3D9Display(egl);
                    using (var ctx = sharedDisplay.CreateContext(new EglContextOptions()))
                        ctx.MakeCurrent().Dispose();

                    return new AngleWin32PlatformGraphics(egl, sharedDisplay);
                }
                catch (Exception e)
                {
                    sharedDisplay?.Dispose();
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")
                        ?.Log(null, "Unable to initialize ANGLE-based rendering with DirectX9 : {0}", e);
                }
            }
        return null;
    }
}
