using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer.Output
{
    public interface IGlOutputBackend : IOutputBackend
    {
        public IPlatformGraphics PlatformGraphics { get; }
    }
}
