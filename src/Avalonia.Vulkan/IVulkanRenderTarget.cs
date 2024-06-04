using System;
using Avalonia.Metadata;

namespace Avalonia.Vulkan;

[NotClientImplementable]
public interface IVulkanRenderTarget : IDisposable
{
    IVulkanRenderSession BeginDraw();
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