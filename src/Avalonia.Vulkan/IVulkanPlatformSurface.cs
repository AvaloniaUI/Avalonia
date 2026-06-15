using System;
using Avalonia.Platform.Surfaces;

namespace Avalonia.Vulkan;
public interface IVulkanKhrSurfacePlatformSurface : IDisposable, IPlatformRenderSurface
{
    double Scaling { get; }
    PixelSize Size { get; }
    ulong CreateSurface(IVulkanPlatformGraphicsContext context);
}

public interface IVulkanKhrSurfacePlatformSurfaceFactory
{
    bool CanRenderToSurface(IVulkanPlatformGraphicsContext context, IPlatformRenderSurface surface);
    IVulkanKhrSurfacePlatformSurface CreateSurface(IVulkanPlatformGraphicsContext context, IPlatformRenderSurface surface);
}