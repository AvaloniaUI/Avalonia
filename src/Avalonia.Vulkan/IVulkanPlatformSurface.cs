using System;
using Avalonia.Metadata;
using Avalonia.Platform.Surfaces;

namespace Avalonia.Vulkan;
public interface IVulkanKhrSurfacePlatformSurface : IDisposable, IPlatformRenderSurface
{
    double Scaling { get; }
    PixelSize Size { get; }
    ulong CreateSurface(IVulkanPlatformGraphicsContext context);
}

/// <summary>
/// A platform render surface that provides its own <see cref="IVulkanRenderTarget"/>,
/// e.g. an offscreen image-based target that doesn't involve a swapchain.
/// </summary>
[PrivateApi]
public interface IVulkanRenderTargetPlatformSurface : IPlatformRenderSurface
{
    IVulkanRenderTarget CreateRenderTarget(IVulkanPlatformGraphicsContext context);
}

public interface IVulkanKhrSurfacePlatformSurfaceFactory
{
    bool CanRenderToSurface(IVulkanPlatformGraphicsContext context, IPlatformRenderSurface surface);
    IVulkanKhrSurfacePlatformSurface CreateSurface(IVulkanPlatformGraphicsContext context, IPlatformRenderSurface surface);
}