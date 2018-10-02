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
}
