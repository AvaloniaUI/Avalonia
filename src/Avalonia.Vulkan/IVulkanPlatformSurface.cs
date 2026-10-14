using System;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.Vulkan;
public interface IVulkanKhrSurfacePlatformSurface : IDisposable, IPlatformRenderSurface
{
    double Scaling { get; }
    PixelSize Size { get; }
    ulong CreateSurface(IVulkanPlatformGraphicsContext context);
}

/// <summary>
/// Lets the platform adjust the native surface to the scene that is about to be rendered, e.g. resize a child
/// window used as the swapchain target. Called on the render thread before the swapchain is validated for the frame.
/// The returned disposable, if any, is disposed once the frame has been presented.
/// </summary>
//TODO13: merge
[PrivateApi]
public interface IVulkanKhrSurfacePlatformSurfaceWithBeginDraw : IVulkanKhrSurfacePlatformSurface
{
    IDisposable? BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo);
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