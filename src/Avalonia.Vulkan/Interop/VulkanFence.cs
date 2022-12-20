using System;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal struct VulkanFence : IDisposable
{
    private readonly VulkanDeviceApi _api;
    private readonly IVulkanDevice _device;
    private VkFence _handle;
    
    public VulkanFence(VulkanDeviceApi api, IVulkanDevice device, IntPtr handle)
    {
        _api = api;
        _device = device;
        _handle = handle;
    }

    public VulkanFence(IVulkanDevice device, VulkanDeviceApi api, VkFenceCreateFlags flags)
    {
        _device = device;
        _api = api;
        var fenceCreateInfo = new VkFenceCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
            flags = flags
        };
        
        _api.CreateFence(_device.Handle, ref fenceCreateInfo, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateFence");
    }

    public IntPtr Handle => _handle;
    
    public void Dispose()
    {
        _api.DestroyFence(_device.Handle, _handle, IntPtr.Zero);
        _handle = IntPtr.Zero;
    }

    public unsafe void Wait(ulong timeout = ulong.MaxValue)
    {
        VkFence fence = _handle;
        _api.WaitForFences(_device.Handle, 1, &fence, 1, timeout).ThrowOnError("vkWaitForFences");
    }
}