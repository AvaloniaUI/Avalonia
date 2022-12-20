using System;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

class VulkanSemaphore : IDisposable
{
    private readonly IVulkanDevice _device;
    private readonly VulkanDeviceApi _api;
    private VkSemaphore _handle;
    public VkSemaphore Handle => _handle;

    public VulkanSemaphore(IVulkanDevice device, VulkanDeviceApi api)
    {
        _device = device;
        _api = api;
        var info = new VkSemaphoreCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO
        };
        _api.CreateSemaphore(device.Handle, ref info, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateSemaphore");
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            _api.DestroySemaphore(_device.Handle, _handle, IntPtr.Zero);
            _handle = IntPtr.Zero;
        }
    }
}

internal class VulkanSemaphorePair : IDisposable
{

    public unsafe VulkanSemaphorePair(IVulkanDevice device, VulkanDeviceApi api)
    {   
        ImageAvailableSemaphore = new VulkanSemaphore(device, api);
        RenderFinishedSemaphore = new VulkanSemaphore(device, api);
    }

    internal VulkanSemaphore ImageAvailableSemaphore { get; }
    internal VulkanSemaphore RenderFinishedSemaphore { get; }

    public void Dispose()
    {
        ImageAvailableSemaphore.Dispose();
        RenderFinishedSemaphore.Dispose();
    }
}