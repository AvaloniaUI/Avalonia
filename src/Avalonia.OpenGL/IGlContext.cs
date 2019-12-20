using System;

namespace Avalonia.OpenGL
{
    public interface IGlContext : IDisposable
    {
        IGlDisplay Display { get; }
        IDisposable MakeCurrent();
    }
}
