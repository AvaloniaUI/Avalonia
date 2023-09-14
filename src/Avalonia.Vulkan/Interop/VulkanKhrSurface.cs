using System;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;


internal class VulkanKhrSurface : IDisposable
{
    private readonly IVulkanPlatformGraphicsContext _context;
    private readonly IVulkanKhrSurfacePlatformSurface _surfaceInfo;
    private VkSurfaceKHR _handle;
    public VkSurfaceKHR Handle => _handle;

    public VulkanKhrSurface(IVulkanPlatformGraphicsContext context, IVulkanKhrSurfacePlatformSurface surfaceInfo)
    {
        _context = context;
        _surfaceInfo = surfaceInfo;
        _handle = new VkSurfaceKHR(surfaceInfo.CreateSurface(context));
    }
    
    internal bool CanSurfacePresent()
    {
        _context.InstanceApi.GetPhysicalDeviceSurfaceSupportKHR(_context.PhysicalDeviceHandle,
            _context.GraphicsQueueFamilyIndex, _handle, out var isSupported)
            .ThrowOnError("vkGetPhysicalDeviceSurfaceSupportKHR");
        return isSupported != 0;
    }

    internal unsafe VkSurfaceFormatKHR GetSurfaceFormat()
    {
        uint surfaceFormatsCount = 0;
        _context.InstanceApi.GetPhysicalDeviceSurfaceFormatsKHR(_context.PhysicalDeviceHandle,
            _handle, ref surfaceFormatsCount, null)
            .ThrowOnError("vkGetPhysicalDeviceSurfaceFormatsKHR");

        if (surfaceFormatsCount == 0)
            throw new VulkanException("vkGetPhysicalDeviceSurfaceFormatsKHR returned 0 formats");

        var surfaceFormats = stackalloc VkSurfaceFormatKHR[(int)surfaceFormatsCount];
        _context.InstanceApi.GetPhysicalDeviceSurfaceFormatsKHR(_context.PhysicalDeviceHandle,
                _handle, ref surfaceFormatsCount, surfaceFormats)
            .ThrowOnError("vkGetPhysicalDeviceSurfaceFormatsKHR");
        
        
        if (surfaceFormatsCount == 1 && surfaceFormats[0].format == VkFormat.VK_FORMAT_UNDEFINED)
            return new VkSurfaceFormatKHR
            {
                format = VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
                colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR
            };
        for (var c = 0; c < surfaceFormatsCount; c++)
        {
            if (surfaceFormats[c].format == VkFormat.VK_FORMAT_B8G8R8A8_UNORM
                && surfaceFormats[c].colorSpace == VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR)
                return surfaceFormats[c];
        }

        return surfaceFormats[0];
    }

    public PixelSize Size => _surfaceInfo.Size;

    public void Dispose()
    {
        if (_handle.Handle != 0)
            _context.InstanceApi.DestroySurfaceKHR(_context.InstanceHandle, _handle, IntPtr.Zero);
        _handle = default;
    }
}