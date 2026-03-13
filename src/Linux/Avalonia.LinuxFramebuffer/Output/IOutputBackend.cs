using Avalonia.Platform.Surfaces;

namespace Avalonia.LinuxFramebuffer.Output
{
    public interface IOutputBackend : IPlatformRenderSurface
    {
        PixelSize PixelSize { get; }
        double Scaling { get; set; }
    }
}
