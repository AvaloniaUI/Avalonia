using System;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

internal class VulkanCommandBuffer : IDisposable
{
    private readonly VulkanCommandBufferPool _pool;
    private IntPtr _handle;
    private readonly IVulkanDevice _device;
    private readonly VulkanDeviceApi _api;
    private VulkanFence _fence;
    private bool _hasEnded;
    private bool _hasStarted;
    public IntPtr Handle => _handle;

    public VulkanCommandBuffer(VulkanCommandBufferPool pool, IntPtr handle, IVulkanDevice device, VulkanDeviceApi api)
    {
        _pool = pool;
        _handle = handle;
        _device = device;
        _api = api;
        _fence = new VulkanFence(_device, _api, VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT);
    }

    public unsafe void Dispose()
    {
        if (_fence.Handle != IntPtr.Zero)
            _fence.Wait();
        _fence.Dispose();
        if (_handle != IntPtr.Zero)
        {
            VkCommandBuffer buf = _handle;
            _api.FreeCommandBuffers(_device.Handle, _handle, 1, &buf);
            _handle = IntPtr.Zero;
        }

        _pool.OnCommandBufferDisposed(this);
    }

    public void BeginRecording()
    {
        if (_hasStarted)
            return;

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
        };

        _api.BeginCommandBuffer(_handle, ref beginInfo).ThrowOnError("vkBeginCommandBuffer");
        _hasStarted = true;
    }

    public void EndRecording()
    {
        if (_hasStarted && !_hasEnded)
        {
            _api.EndCommandBuffer(_handle).ThrowOnError("vkEndCommandBuffer");
            _hasEnded = true;
        }
    }

    public void Submit()
    {
        Submit(null, null, null);
    }
    
    public unsafe void Submit(
        ReadOnlySpan<VulkanSemaphore> waitSemaphores,
        ReadOnlySpan<VkPipelineStageFlags> waitDstStageMask,
        ReadOnlySpan<VulkanSemaphore> signalSemaphores,
        VulkanFence? fence = null)
    {
        
        EndRecording();
        VkFence fenceHandle = (fence ?? _fence).Handle;
        _api.ResetFences(_device.Handle, 1, &fenceHandle)
            .ThrowOnError("vkResetFences");
        
        var pWaitSempaphores = stackalloc IntPtr[waitSemaphores.Length];
        for (var c = 0; c < waitSemaphores.Length; c++)
            pWaitSempaphores[c] = waitSemaphores[c].Handle;
        
        var pSignalSemaphores = stackalloc IntPtr[signalSemaphores.Length];
        for (var c = 0; c < signalSemaphores.Length; c++)
            pSignalSemaphores[c] = signalSemaphores[c].Handle;

        IntPtr commandBuffer = _handle;
        fixed (VkPipelineStageFlags* flags = waitDstStageMask)
        {
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                waitSemaphoreCount = (uint)waitSemaphores.Length,
                pWaitSemaphores = pWaitSempaphores,
                signalSemaphoreCount = (uint)signalSemaphores.Length,
                pSignalSemaphores = pSignalSemaphores,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
                pWaitDstStageMask = flags
            };
            _api.QueueSubmit(_device.MainQueueHandle, 1, &submitInfo, fenceHandle)
                .ThrowOnError("vkQueueSubmit");
        }
    }
}