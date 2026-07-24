using System;
using Avalonia.Platform;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Skia.RenderTests;

/// <summary>
/// An offscreen Vulkan surface that renders into a VkImage and reads the pixels back
/// into a writeable bitmap at the end of each rendering session.
/// </summary>
internal class VulkanReadbackPlatformSurface : IVulkanRenderTargetPlatformSurface
{
    private readonly IWriteableBitmapImpl _bitmap;
    private readonly double _scaling;

    public VulkanReadbackPlatformSurface(IWriteableBitmapImpl bitmap, double scaling)
    {
        _bitmap = bitmap;
        _scaling = scaling;
    }

    public IVulkanRenderTarget CreateRenderTarget(IVulkanPlatformGraphicsContext context) =>
        new RenderTarget(context, _bitmap, _scaling);

    private class RenderTarget : IVulkanRenderTarget
    {
        private readonly IVulkanPlatformGraphicsContext _context;
        private readonly IWriteableBitmapImpl _bitmap;
        private readonly double _scaling;
        private readonly VulkanCommandBufferPool _pool;
        private VulkanImage? _image;

        public RenderTarget(IVulkanPlatformGraphicsContext context, IWriteableBitmapImpl bitmap, double scaling)
        {
            _context = context;
            _bitmap = bitmap;
            _scaling = scaling;
            _pool = new VulkanCommandBufferPool(context);
        }

        public IVulkanRenderSession BeginDraw()
        {
            var l = _context.EnsureCurrent();
            try
            {
                _pool.FreeUsedCommandBuffers();
                var size = _bitmap.PixelSize;
                if (_image == null || _image.Size != size)
                {
                    DestroyImage();
                    _image = new VulkanImage(_context, _pool, VkFormat.VK_FORMAT_R8G8B8A8_UNORM, size, 1);
                }
                else
                    _image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                        VkAccessFlags.VK_ACCESS_NONE);

                return new Session(this, _image, l);
            }
            catch
            {
                l.Dispose();
                throw;
            }
        }

        private void DestroyImage()
        {
            if (_image == null)
                return;
            _context.DeviceApi.DeviceWaitIdle(_context.DeviceHandle);
            _image.Dispose();
            _image = null;
        }

        public void Dispose()
        {
            using (_context.EnsureCurrent())
            {
                DestroyImage();
                _pool.Dispose();
            }
        }

        private unsafe void ReadPixels(VulkanImage image)
        {
            image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT);

            var api = _context.DeviceApi;
            var device = _context.DeviceHandle;
            var size = image.Size;
            var tightStride = size.Width * 4;
            var byteSize = (ulong)(tightStride * size.Height);

            var bufferCreateInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
                size = byteSize,
                usage = VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
            };
            api.CreateBuffer(device, ref bufferCreateInfo, IntPtr.Zero, out var buffer)
                .ThrowOnError("vkCreateBuffer");
            var memory = default(VkDeviceMemory);
            try
            {
                api.GetBufferMemoryRequirements(device, buffer, out var memoryRequirements);
                var allocateInfo = new VkMemoryAllocateInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                    allocationSize = memoryRequirements.size,
                    memoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(_context,
                        memoryRequirements.memoryTypeBits,
                        VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
                        VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
                };
                api.AllocateMemory(device, ref allocateInfo, IntPtr.Zero, out memory)
                    .ThrowOnError("vkAllocateMemory");
                api.BindBufferMemory(device, buffer, memory, 0).ThrowOnError("vkBindBufferMemory");

                var commandBuffer = _pool.CreateCommandBuffer();
                commandBuffer.BeginRecording();
                var region = new VkBufferImageCopy
                {
                    imageSubresource =
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                        layerCount = 1
                    },
                    imageExtent =
                    {
                        width = (uint)size.Width,
                        height = (uint)size.Height,
                        depth = 1
                    }
                };
                api.CmdCopyImageToBuffer(commandBuffer.Handle, image.Handle,
                    VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, buffer, 1, &region);
                commandBuffer.EndRecording();
                commandBuffer.Submit();
                api.QueueWaitIdle(_context.MainQueueHandle).ThrowOnError("vkQueueWaitIdle");

                api.MapMemory(device, memory, 0, byteSize, 0, out var mapped).ThrowOnError("vkMapMemory");
                try
                {
                    using (var fb = _bitmap.Lock())
                        for (var y = 0; y < size.Height; y++)
                            Buffer.MemoryCopy(
                                (byte*)mapped + y * tightStride,
                                (byte*)fb.Address + y * fb.RowBytes,
                                fb.RowBytes, tightStride);
                }
                finally
                {
                    api.UnmapMemory(device, memory);
                }
            }
            finally
            {
                api.DestroyBuffer(device, buffer, IntPtr.Zero);
                if (memory.Handle != 0)
                    api.FreeMemory(device, memory, IntPtr.Zero);
            }
        }

        private class Session : IVulkanRenderSession
        {
            private readonly RenderTarget _target;
            private readonly VulkanImage _image;
            private readonly IDisposable _lock;

            public Session(RenderTarget target, VulkanImage image, IDisposable @lock)
            {
                _target = target;
                _image = image;
                _lock = @lock;
            }

            public double Scaling => _target._scaling;
            public PixelSize Size => _image.Size;
            public bool IsYFlipped => true;
            public VulkanImageInfo ImageInfo => _image.ImageInfo;
            public bool IsRgba => true;

            public void Dispose()
            {
                try
                {
                    _target.ReadPixels(_image);
                }
                finally
                {
                    _lock.Dispose();
                }
            }
        }
    }
}
