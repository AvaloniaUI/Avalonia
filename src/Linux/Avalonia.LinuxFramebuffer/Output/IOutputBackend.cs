using Avalonia.Skia;

namespace Avalonia.LinuxFramebuffer.Output
{
    public interface IOutputBackend : ISurfaceOrientation
    {
        PixelSize PixelSize { get; }
        double Scaling { get; set; }
    }
}
