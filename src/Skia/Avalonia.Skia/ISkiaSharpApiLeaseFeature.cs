using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

[Unstable]
public interface ISkiaSharpApiLeaseFeature
{
    public ISkiaSharpApiLease Lease();
}

[Unstable]
public interface ISkiaSharpApiLease : IDisposable
{
    SKCanvas SkCanvas { get; }
    GRContext? GrContext { get; }
    SKSurface? SkSurface { get; }
    double CurrentOpacity { get; }
    ISkiaSharpPlatformGraphicsApiLease? TryLeasePlatformGraphicsApi();
}

[Unstable]
public interface ISkiaSharpPlatformGraphicsApiLease : IDisposable
{
    IPlatformGraphicsContext Context { get; }
}
