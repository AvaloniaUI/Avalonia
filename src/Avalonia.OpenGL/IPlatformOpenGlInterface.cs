using Avalonia.Platform;

namespace Avalonia.OpenGL
{
    public interface IPlatformOpenGlInterface : IPlatformGpu
    {
        new IGlContext PrimaryContext { get; }
        IGlContext CreateSharedContext();
        bool CanShareContexts { get; }
        bool CanCreateContexts { get; }
        IGlContext CreateContext();
        /*IGlContext TryCreateContext(GlVersion version);
        */
    }
}
