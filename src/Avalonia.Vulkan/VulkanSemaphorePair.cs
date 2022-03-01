using System;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    internal class VulkanSemaphorePair : IDisposable
    {
        private readonly VulkanDevice _device;

        public unsafe VulkanSemaphorePair(VulkanDevice device)
        {
            _device = device;

            var semaphoreCreateInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };

            _device.Api.CreateSemaphore(_device.InternalHandle, semaphoreCreateInfo, null, out var semaphore).ThrowOnError();
            ImageAvailableSemaphore = semaphore;

            _device.Api.CreateSemaphore(_device.InternalHandle, semaphoreCreateInfo, null, out semaphore).ThrowOnError();
            RenderFinishedSemaphore = semaphore;
        }

        internal Semaphore ImageAvailableSemaphore { get; }
        internal Semaphore RenderFinishedSemaphore { get; }

        public unsafe void Dispose()
        {
            _device.Api.DestroySemaphore(_device.InternalHandle, ImageAvailableSemaphore, null);
            _device.Api.DestroySemaphore(_device.InternalHandle, RenderFinishedSemaphore, null);
        }
    }
}
