using Avalonia.Skia;

namespace Avalonia.LinuxFramebuffer.Input
{
    public interface IScreenInfoProvider : ISurfaceOrientation
    {
        Size ScaledSize { get; }
    }
}
