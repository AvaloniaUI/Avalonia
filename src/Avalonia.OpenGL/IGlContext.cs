namespace Avalonia.OpenGL
{
    public interface IGlContext
    {
        IGlDisplay Display { get; }
        void MakeCurrent(IGlSurface surface);
    }
}