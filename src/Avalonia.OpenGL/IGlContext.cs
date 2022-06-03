using System;
using Avalonia.Platform;

namespace Avalonia.OpenGL
{
    public interface IGlContext : IPlatformGpuContext
    {
        GlVersion Version { get; }
        GlInterface GlInterface { get; }
        int SampleCount { get; }
        int StencilSize { get; }
        IDisposable MakeCurrent();
        IDisposable EnsureCurrent();
        bool IsSharedWith(IGlContext context);
    }
}
