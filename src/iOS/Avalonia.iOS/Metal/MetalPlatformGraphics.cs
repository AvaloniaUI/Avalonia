using System;
using Avalonia.Platform;
using Metal;
using SkiaSharp;

namespace Avalonia.iOS;
#nullable enable

internal class MetalPlatformGraphics : IPlatformGraphics
{
    private MetalPlatformGraphics()
    {
        
    }
    
    public bool UsesSharedContext => false;
    public IPlatformGraphicsContext CreateContext() => new MetalDevice(MTLDevice.SystemDefault);

    public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();

    public static MetalPlatformGraphics? TryCreate()
    {
        var device = MTLDevice.SystemDefault;
        if (device is null)
        {
            // Can be null on unsupported OS versions.
            return null;
        }

#if !TVOS
        using var queue = device.CreateCommandQueue();
        using var context = GRContext.CreateMetal(new GRMtlBackendContext { Device = device, Queue = queue });
        if (context is null)
        {
            // Can be null on macCatalyst because of older Skia bug.
            // Fixed in SkiaSharp 3.0
            return null;
        }
#endif

        return new MetalPlatformGraphics();
    }
}
