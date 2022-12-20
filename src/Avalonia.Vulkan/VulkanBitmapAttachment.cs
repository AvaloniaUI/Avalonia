using System;
using Avalonia.Utilities;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal class VulkanBitmapAttachment
{
    private readonly DisposableLock _lock = new();
    private VulkanImage? _image;
    public ulong Image => (_image ?? throw new ObjectDisposedException(nameof(VulkanBitmapAttachment))).Handle.Handle;

    internal VulkanBitmapAttachment(IVulkanPlatformGraphicsContext context,
        VulkanCommandBufferPool pool,
        VkFormat format, PixelSize size)
    {
        _image = new VulkanImage(context, pool, format, size, 1);
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }

    public void Present()
    {
        if (_image == null)
            throw new ObjectDisposedException(nameof(VulkanBitmapAttachment));
        _image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, VkAccessFlags.VK_ACCESS_NONE);
    }

    public IDisposable Lock() => _lock.Lock();
}