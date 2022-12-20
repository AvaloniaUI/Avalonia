using System;
using System.Collections.Generic;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

internal class VulkanCommandBufferPool : IDisposable
{
    private readonly IVulkanPlatformGraphicsContext _context;
    private readonly List<VulkanCommandBuffer> _commandBuffers = new();
    private VkCommandPool _handle;
    public VkCommandPool Handle => _handle;
    private bool _clearingCommandBuffers = false;

    public VulkanCommandBufferPool(IVulkanPlatformGraphicsContext context)
    {
        _context = context;
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
        try
        {
            _clearingCommandBuffers = true;
            foreach (var usedCommandBuffer in _commandBuffers)
                usedCommandBuffer.Dispose();
            _commandBuffers.Clear();
        }
        finally
        {
            _clearingCommandBuffers = false;
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

        var rv = new VulkanCommandBuffer(this, bufferHandle, _context);
        _commandBuffers.Add(rv);
        return rv;
    }

    internal void OnCommandBufferDisposed(VulkanCommandBuffer buffer)
    {
        if (!_clearingCommandBuffers)
            _commandBuffers.Remove(buffer);
    }
}