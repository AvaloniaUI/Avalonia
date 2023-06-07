using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IPlatformGraphics
{
    bool UsesSharedContext { get; }
    IPlatformGraphicsContext CreateContext();
    IPlatformGraphicsContext GetSharedContext();
}

[Unstable]
public interface IPlatformGraphicsContext : IDisposable, IOptionalFeatureProvider
{
    bool IsLost { get; }
    IDisposable EnsureCurrent();
}

public class PlatformGraphicsContextLostException : Exception
{
    
}