namespace Avalonia.OpenGL
{
    public interface IPlatformOpenGlInterface
    {
        IGlContext PrimaryContext { get; }
        IGlContext CreateSharedContext();
        bool CanShareContexts { get; }
        bool CanCreateContexts { get; }
        IGlContext CreateContext();
        /*IGlContext TryCreateContext(GlVersion version);
        */
    }
}
