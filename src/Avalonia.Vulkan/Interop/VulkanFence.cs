using System;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal struct VulkanFence : IDisposable
{
    private readonly IVulkanPlatformGraphicsContext _context;
    private VkFence _handle;

    public VulkanFence(IVulkanPlatformGraphicsContext context, VkFenceCreateFlags flags)
    {
        _context = context;
        var fenceCreateInfo = new VkFenceCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
            flags = flags
        };
        
        _context.DeviceApi.CreateFence(_context.DeviceHandle, ref fenceCreateInfo, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateFence");
    }

    public VkFence Handle => _handle;
    
    public void Dispose()
    {
        _context.DeviceApi.DestroyFence(_context.DeviceHandle, _handle, IntPtr.Zero);
        _handle = default;
    }

    public bool IsSignaled => _context.DeviceApi.GetFenceStatus(_context.DeviceHandle, _handle) == VkResult.VK_SUCCESS;
    
    public unsafe void Wait(ulong timeout = ulong.MaxValue)
    {
        VkFence fence = _handle;
        _context.DeviceApi.WaitForFences(_context.DeviceHandle, 1, &fence, 1, timeout).ThrowOnError("vkWaitForFences");
    }
}