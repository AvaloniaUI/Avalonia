using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan
{
    public class VulkanDevice : IDisposable
    {
        private static object _lock = new object();

        private VulkanDevice(Device apiHandle, VulkanPhysicalDevice physicalDevice, Vk api)
        {
            InternalHandle = apiHandle;
            Api = api;

            api.GetDeviceQueue(apiHandle, physicalDevice.QueueFamilyIndex, 0, out var queue);

            var vulkanQueue = new VulkanQueue(this, queue);
            Queue = vulkanQueue;

            PresentQueue = vulkanQueue;

            CommandBufferPool = new VulkanCommandBufferPool(this, physicalDevice);
        }

        public IntPtr Handle => InternalHandle.Handle;

        internal Device InternalHandle { get; }
        public Vk Api { get; }

        public VulkanQueue Queue { get; private set; }
        public VulkanQueue PresentQueue { get; }
        public VulkanCommandBufferPool CommandBufferPool { get; }

        internal static List<string> RequiredDeviceExtensions { get; } = new() { KhrSwapchain.ExtensionName };

        public void Dispose()
        {
            WaitIdle();
            CommandBufferPool?.Dispose();
            Queue = null;
        }

        public static unsafe VulkanDevice Create(VulkanInstance instance, VulkanPhysicalDevice physicalDevice,
            VulkanOptions options)
        {
            if(options.VulkanDeviceInitialization != null)
            {
                return new VulkanDevice(options.VulkanDeviceInitialization.CreateDevice(instance.Api, instance, physicalDevice, options), physicalDevice, instance.Api);
            }
            else
                throw new Exception("VulkanDeviceInitialization is not found. Device can't be created.");
        }

        internal void Submit(SubmitInfo submitInfo, Fence fence = new())
        {
            lock (_lock)
            {
                Api.QueueSubmit(Queue.InternalHandle, 1, submitInfo, fence);
            }
        }

        public void WaitIdle()
        { 
            lock (_lock)
            {
                Api.DeviceWaitIdle(InternalHandle);
            }
        }

        public void QueueWaitIdle()
        {
            lock (_lock)
            {
                Api.QueueWaitIdle(Queue.InternalHandle);
            }
        }

        public object Lock => _lock;
    }
}
