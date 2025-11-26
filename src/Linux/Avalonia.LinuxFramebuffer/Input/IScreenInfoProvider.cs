using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer.Input
{
    public interface IScreenInfoProvider
    {
        Size ScaledSize { get; }
        SurfaceOrientation Orientation { get; }
    }
}
