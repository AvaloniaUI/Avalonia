namespace Avalonia.OpenGL
{
    public interface IWindowingPlatformGlFeature
    {
        IGlContext CreateContext();
        IGlContext MainContext { get; }
    }
}
