using System;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

class VulkanSemaphore : IDisposable
{
    private readonly IVulkanPlatformGraphicsContext _context;

    private VkSemaphore _handle;
    public VkSemaphore Handle => _handle;

    public VulkanSemaphore(IVulkanPlatformGraphicsContext context)
    {
        _context = context;
        var info = new VkSemaphoreCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO
        };
        _context.DeviceApi.CreateSemaphore(context.DeviceHandle, ref info, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateSemaphore");
    }

    public VulkanSemaphore(IVulkanPlatformGraphicsContext context, VkSemaphore handle)
    {
        _context = context;
        _handle = handle;
    }

    public void Dispose()
    {
        if (_handle.Handle != 0)
        {
            _context.DeviceApi.DestroySemaphore(_context.DeviceHandle, _handle, IntPtr.Zero);
            _handle = default;
        }
    }
}

internal class VulkanSemaphorePair : IDisposable
{

    public unsafe VulkanSemaphorePair(IVulkanPlatformGraphicsContext context)
    {   
        ImageAvailableSemaphore = new VulkanSemaphore(context);
        RenderFinishedSemaphore = new VulkanSemaphore(context);
    }

    internal VulkanSemaphore ImageAvailableSemaphore { get; }
    internal VulkanSemaphore RenderFinishedSemaphore { get; }

    public void Dispose()
    {
        ImageAvailableSemaphore.Dispose();
        RenderFinishedSemaphore.Dispose();
    }
}