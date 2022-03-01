using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanImage : IDisposable
    {
        private readonly VulkanDevice _device;
        private readonly VulkanPhysicalDevice _physicalDevice;
        private readonly VulkanCommandBufferPool _commandBufferPool;
        private ImageLayout _currentLayout;
        private AccessFlags _currentAccessFlags;
        private ImageUsageFlags _imageUsageFlags { get; }
        private ImageView? _imageView { get; set; }
        private DeviceMemory _imageMemory { get; set; }
        
        internal Image? InternalHandle { get; private set; }
        internal Format Format { get; }
        internal ImageAspectFlags AspectFlags { get; private set; }
        
        public ulong Handle => InternalHandle?.Handle ?? 0;
        public ulong ViewHandle => _imageView?.Handle ?? 0;
        public uint UsageFlags => (uint) _imageUsageFlags;
        public ulong MemoryHandle => _imageMemory.Handle;
        public uint MipLevels { get; private set; }
        public PixelSize Size { get; }
        public ulong MemorySize { get; private set; }
        public uint CurrentLayout => (uint) _currentLayout;

        public VulkanImage(VulkanDevice device, VulkanPhysicalDevice physicalDevice, VulkanCommandBufferPool commandBufferPool, uint format, PixelSize size, uint mipLevels = 0)
        {
            _device = device;
            _physicalDevice = physicalDevice;
            _commandBufferPool = commandBufferPool;
            Format = (Format) format;
            Size = size;
            MipLevels = mipLevels;
            _imageUsageFlags =
                ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferDstBit |
                ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageSampledBit;

            Initialize();
        }

        public unsafe void Initialize()
        {
            if (!InternalHandle.HasValue)
            {
                MipLevels = MipLevels != 0 ? MipLevels : (uint)Math.Floor(Math.Log(Math.Max(Size.Width, Size.Height), 2));

                var imageCreateInfo = new ImageCreateInfo
                {
                    SType = StructureType.ImageCreateInfo,
                    ImageType = ImageType.ImageType2D,
                    Format = Format,
                    Extent =
                        new Extent3D((uint?)Size.Width,
                            (uint?)Size.Height, 1),
                    MipLevels = MipLevels,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.SampleCount1Bit,
                    Tiling = Tiling,
                    Usage = _imageUsageFlags,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined,
                    Flags = ImageCreateFlags.ImageCreateMutableFormatBit
                };

                _device.Api
                    .CreateImage(_device.InternalHandle, imageCreateInfo, null, out var image).ThrowOnError();
                InternalHandle = image;

                _device.Api.GetImageMemoryRequirements(_device.InternalHandle, InternalHandle.Value,
                    out var memoryRequirements);

                var memoryAllocateInfo = new MemoryAllocateInfo
                {
                    SType = StructureType.MemoryAllocateInfo,
                    AllocationSize = memoryRequirements.Size,
                    MemoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(
                        _physicalDevice,
                        memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit)
                };

                _device.Api.AllocateMemory(_device.InternalHandle, memoryAllocateInfo, null,
                    out var imageMemory);

                _imageMemory = imageMemory;

                _device.Api.BindImageMemory(_device.InternalHandle, InternalHandle.Value, _imageMemory, 0);

                MemorySize = memoryRequirements.Size;

                var componentMapping = new ComponentMapping(
                    ComponentSwizzle.Identity,
                    ComponentSwizzle.Identity,
                    ComponentSwizzle.Identity,
                    ComponentSwizzle.Identity);

                AspectFlags = ImageAspectFlags.ImageAspectColorBit;

                var subresourceRange = new ImageSubresourceRange(AspectFlags, 0, MipLevels, 0, 1);

                var imageViewCreateInfo = new ImageViewCreateInfo
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = InternalHandle.Value,
                    ViewType = ImageViewType.ImageViewType2D,
                    Format = Format,
                    Components = componentMapping,
                    SubresourceRange = subresourceRange
                };

                _device.Api
                    .CreateImageView(_device.InternalHandle, imageViewCreateInfo, null, out var imageView)
                    .ThrowOnError();

                _imageView = imageView;

                _currentLayout = ImageLayout.Undefined;

                TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.AccessNoneKhr);
            }
        }

        public ImageTiling Tiling => ImageTiling.Optimal;

        internal void TransitionLayout(ImageLayout destinationLayout, AccessFlags destinationAccessFlags)
        {
            var commandBuffer = _commandBufferPool.CreateCommandBuffer();
            commandBuffer.BeginRecording();

            VulkanMemoryHelper.TransitionLayout(_device, commandBuffer.InternalHandle, InternalHandle.Value,
                _currentLayout,
                _currentAccessFlags,
                destinationLayout, destinationAccessFlags,
                MipLevels);

            commandBuffer.EndRecording();

            commandBuffer.Submit();

            _currentLayout = destinationLayout;
            _currentAccessFlags = destinationAccessFlags;
        }

        public void TransitionLayout(uint destinationLayout, uint destinationAccessFlags)
        {
            TransitionLayout((ImageLayout)destinationLayout, (AccessFlags)destinationAccessFlags);
        }

        public unsafe void Dispose()
        {
            _device.Api.DestroyImageView(_device.InternalHandle, _imageView.Value, null);
            _device.Api.DestroyImage(_device.InternalHandle, InternalHandle.Value, null);
            _device.Api.FreeMemory(_device.InternalHandle, _imageMemory, null);

            _imageView = default;
            InternalHandle = default;
            _imageMemory = default;
        }
    }
}
