namespace Avalonia.OpenGL
{
    public interface IGlDisplay
    {
        GlDisplayType Type { get; }
        GlInterface GlInterface { get; }
        int SampleCount { get; }
        int StencilSize { get; }
    }
}
