using System;

namespace Avalonia.OpenGL
{
    public interface IGlContext : IDisposable
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
