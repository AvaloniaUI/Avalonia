using Avalonia.OpenGL;

namespace Avalonia.LinuxFramebuffer.Output
{
    public interface IGlOutputBackend : IOutputBackend
    {
        public IPlatformOpenGlInterface PlatformOpenGlInterface { get; }
    }
}
