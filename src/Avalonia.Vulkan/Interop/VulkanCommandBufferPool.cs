using System;
using System.Collections.Generic;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

internal class VulkanCommandBufferPool : IDisposable
{
    private readonly IVulkanPlatformGraphicsContext _context;
    private readonly bool _autoFree;
    private readonly Queue<VulkanCommandBuffer> _commandBuffers = new();
    private VkCommandPool _handle;
    public VkCommandPool Handle => _handle;

    public VulkanCommandBufferPool(IVulkanPlatformGraphicsContext context, bool autoFree = false)
    {
        _context = context;
        _autoFree = autoFree;
        var createInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = context.GraphicsQueueFamilyIndex
        };
        _context.DeviceApi.CreateCommandPool(_context.DeviceHandle, ref createInfo, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateCommandPool");
    }

    public void FreeUsedCommandBuffers()
    {
        while (_commandBuffers.Count > 0)
            _commandBuffers.Dequeue().Dispose();
    }
    
    public void FreeFinishedCommandBuffers()
    {
        while (_commandBuffers.Count > 0)
        {
            var next = _commandBuffers.Peek();
            if(!next.IsFinished)
                return;
            _commandBuffers.Dequeue();
            next.Dispose();
        }
    }

    public void Dispose()
    {
        FreeUsedCommandBuffers();
        
        if (_handle.Handle != 0)
            _context.DeviceApi.DestroyCommandPool(_context.DeviceHandle, _handle, IntPtr.Zero);
        _handle = default;
    }

    public unsafe VulkanCommandBuffer CreateCommandBuffer()
    {
        if (_autoFree)
            FreeFinishedCommandBuffers();
        
        var commandBufferAllocateInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            commandPool = _handle,
            commandBufferCount = 1,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY
        };
        VkCommandBuffer bufferHandle = default;
        _context.DeviceApi.AllocateCommandBuffers(_context.DeviceHandle, ref commandBufferAllocateInfo,
            &bufferHandle).ThrowOnError("vkAllocateCommandBuffers");

        return new VulkanCommandBuffer(this, bufferHandle, _context);
    }
    
    public void AddSubmittedCommandBuffer(VulkanCommandBuffer buffer) => _commandBuffers.Enqueue(buffer);
}