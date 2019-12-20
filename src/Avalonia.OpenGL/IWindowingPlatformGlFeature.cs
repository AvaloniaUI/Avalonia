namespace Avalonia.OpenGL
{
    public interface IWindowingPlatformGlFeature
    {
        IGlDisplay Display { get; }
        IGlContext CreateContext();
    }
}
