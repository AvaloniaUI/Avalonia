using System;
using System.Linq;
using Avalonia.Logging;
using Avalonia.OpenGL.Angle;
using Avalonia.Platform;

namespace Avalonia.Win32.OpenGl.Angle;

internal static class AngleWin32PlatformGraphicsFactory
{
    public static IPlatformGraphics? TryCreate(AngleOptions? options)
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

        var allowedPlatformApis = options?.AllowedPlatformApis ?? new[] { AngleOptions.PlatformApi.DirectX11 };

        foreach (var api in allowedPlatformApis.Distinct())
        {
            switch (api)
            {
                case AngleOptions.PlatformApi.DirectX11
                when D3D11AngleWin32PlatformGraphics.TryCreate(egl) is { } platformGraphics:
                    return platformGraphics;

                case AngleOptions.PlatformApi.DirectX9
                when D3D9AngleWin32PlatformGraphics.TryCreate(egl) is { } platformGraphics:
                    return platformGraphics;

                default:
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")
                        ?.Log(null, "Unknown requested PlatformApi {0}", api);
                    break;
            }
        }

        return null;
    }
}
