using System;
using Silk.NET.OpenGL;

namespace Avalonia.OpenGL
{
    public interface IGlContext : IDisposable
    {
        GlVersion Version { get; }
        GL GL { get; }
        int SampleCount { get; }
        int StencilSize { get; }
        IDisposable MakeCurrent();
        IDisposable EnsureCurrent();
        bool IsSharedWith(IGlContext context);
    }
}
