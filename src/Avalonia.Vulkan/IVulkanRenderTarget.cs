using System;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Metadata;

namespace Avalonia.Vulkan;

[NotClientImplementable]
public interface IVulkanRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
{
    IVulkanRenderSession BeginDraw();
    //TODO13: merge
    IVulkanRenderSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo) => BeginDraw();
}

[NotClientImplementable]
public interface IVulkanRenderSession : IDisposable
{
    double Scaling { get; }
    PixelSize Size { get; }
    public bool IsYFlipped { get; }
    VulkanImageInfo ImageInfo { get; }
    bool IsRgba { get; }
}