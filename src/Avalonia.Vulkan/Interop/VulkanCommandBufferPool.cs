using System;
using System.Collections.Generic;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal class VulkanCommandBufferPool : IDisposable
{
    private readonly IVulkanDevice _device;
    private readonly VulkanDeviceApi _api;
    private readonly List<VulkanCommandBuffer> _commandBuffers = new();
    private VkCommandPool _handle;

    public VulkanCommandBufferPool(IVulkanDevice device, VulkanDeviceApi api)
    {
        _device = device;
        _api = api;
        var createInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = device.GraphicsQueueFamilyIndex
        };
        api.CreateCommandPool(device.Handle, ref createInfo, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateCommandPool");
    }

    public void FreeUsedCommandBuffers()
    {
        foreach (var usedCommandBuffer in _commandBuffers) 
            usedCommandBuffer.Dispose();
        _commandBuffers.Clear();
    }
    
    public void Dispose()
    {
        FreeUsedCommandBuffers();
        
        if (_handle != IntPtr.Zero)
            _api.DestroyCommandPool(_device.Handle, _handle, IntPtr.Zero);
        _handle = IntPtr.Zero;
    }

    public unsafe VulkanCommandBuffer CreateCommandBuffer()
    {
        var commandBufferAllocateInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            commandPool = _handle,
            commandBufferCount = 1,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY
        };
        VkCommandBuffer bufferHandle = default;
        _api.AllocateCommandBuffers(_device.Handle, ref commandBufferAllocateInfo,
            &bufferHandle).ThrowOnError("vkAllocateCommandBuffers");

        return new VulkanCommandBuffer(this, bufferHandle, _device, _api);
    }

    internal void OnCommandBufferDisposed(VulkanCommandBuffer buffer) => _commandBuffers.Remove(buffer);
}