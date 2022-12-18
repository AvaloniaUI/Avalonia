using System;

namespace Avalonia.Vulkan;
public interface IVulkanKhrSurfacePlatformSurface : IDisposable
{
    double Scaling { get; }
    PixelSize Size { get; }
    IntPtr CreateSurface(IVulkanPlatformGraphicsContext context);
}

public interface IVulkanKhrSurfacePlatformSurfaceFactory
{
    bool CanRenderToSurface(IVulkanPlatformGraphicsContext context, object surface);
    IVulkanKhrSurfacePlatformSurface CreateSurface(IVulkanPlatformGraphicsContext context, object surface);
}