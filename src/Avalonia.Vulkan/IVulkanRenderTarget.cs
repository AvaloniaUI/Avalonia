using System;

namespace Avalonia.Vulkan;

public interface IVulkanPlatformSurface
{
    public IVulkanRenderTarget CreateRenderTarget(IVulkanPlatformGraphicsContext context);
}

public interface IVulkanRenderTarget : IDisposable
{
    IVulkanRenderSession BeginDraw();
}

public interface IVulkanRenderSession : IDisposable
{
    double Scaling { get; }
    PixelSize Size { get; }
    public bool IsYFlipped { get; }
    uint ImageFormat { get; }
    IntPtr ImageHandle { get; }
    uint ImageLayout { get; }
    uint ImageTiling { get; }
    uint ImageUsageFlags { get; }
    uint LevelCount { get; }
    IntPtr ImageMemoryHandle { get; }
    ulong ImageMemorySize { get; }
    bool IsRgba { get; }
}