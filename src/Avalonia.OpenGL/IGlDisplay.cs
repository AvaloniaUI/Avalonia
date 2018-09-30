using System;

namespace Avalonia.OpenGL
{
    public interface IGlDisplay
    {
        GlDisplayType Type { get; }
        GlInterface GlInterface { get; }
        void ClearContext();
        int SampleCount { get; }
        int StencilSize { get; }
    }
    
    public enum GlDisplayType
    {
        OpenGL2,
        OpenGLES2
    }

    public interface IGlContext
    {
        IGlDisplay Display { get; }
        void MakeCurrent(IGlSurface surface);
    }

    public interface IGlSurface : IDisposable
    {
        IGlDisplay Display { get; }
        void SwapBuffers();
    }
}
