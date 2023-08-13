using System;

namespace Avalonia.Vulkan;

public interface IVulkanRenderTarget : IDisposable
{
    IVulkanRenderSession BeginDraw();
}

public interface IVulkanRenderSession : IDisposable
{
    double Scaling { get; }
    PixelSize Size { get; }
    public bool IsYFlipped { get; }
    VulkanImageInfo ImageInfo { get; }
    bool IsRgba { get; }
}