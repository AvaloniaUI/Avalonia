using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer.Output
{
    public interface IOutputBackend
    {
        PixelSize PixelSize { get; }
        double Scaling { get; set; }
        SurfaceOrientation Orientation { get; }
    }
}
