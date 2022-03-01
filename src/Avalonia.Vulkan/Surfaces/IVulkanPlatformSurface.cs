using System;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Surfaces
{
    public interface IVulkanPlatformSurface : IDisposable
    {
        float Scaling { get; }
        PixelSize SurfaceSize { get; }
        SurfaceKHR CreateSurface(VulkanInstance instance);
    }
}
