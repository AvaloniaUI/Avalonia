using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
public interface IPlatformGpu
{
    IPlatformGpuContext PrimaryContext { get; }
}

[Unstable]
public interface IPlatformGpuContext : IDisposable
{
    IDisposable EnsureCurrent();
}