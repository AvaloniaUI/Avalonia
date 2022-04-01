using System;
using Avalonia.Utilities;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Controls
{
    public class VulkanBitmapAttachment
    {
        private readonly DisposableLock _lock = new();
        private bool _disposed;

        public VulkanImage Image { get; set; }

        public VulkanBitmapAttachment(VulkanPlatformInterface platformInterface, uint format, PixelSize size)
        {
            Image = new VulkanImage(platformInterface.Device, platformInterface.PhysicalDevice, platformInterface.Device.CommandBufferPool, format, size, 1);
        }

        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
            _disposed = true;
        }

        public void Present()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VulkanBitmapAttachment));
            Image.TransitionLayout(ImageLayout.TransferSrcOptimal, 0);
        }

        public IDisposable Lock() => _lock.Lock();
    }

}
