using System;

namespace Avalonia.OpenGL
{
    public interface IGlSurface : IDisposable
    {
        IGlDisplay Display { get; }
        void SwapBuffers();
    }
}