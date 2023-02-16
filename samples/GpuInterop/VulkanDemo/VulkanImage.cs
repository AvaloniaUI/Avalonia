using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Vulkan;
using SharpDX.DXGI;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using SilkNetDemo;
using SkiaSharp;
using Device = Silk.NET.Vulkan.Device;
using Format = Silk.NET.Vulkan.Format;

namespace GpuInterop.VulkanDemo;

public unsafe class VulkanImage : IDisposable
    {
        private readonly VulkanContext _vk;
        private readonly Instance _instance;
        private readonly Device _device;
        private readonly PhysicalDevice _physicalDevice;
        private readonly VulkanCommandBufferPool _commandBufferPool;
        private ImageLayout _currentLayout;
        private AccessFlags _currentAccessFlags;
        private ImageUsageFlags _imageUsageFlags { get; }
        private ImageView? _imageView { get; set; }
        private DeviceMemory _imageMemory { get; set; }
        private SharpDX.Direct3D11.Texture2D? _d3dTexture2D;
        
        internal Image? InternalHandle { get; private set; }
        internal Format Format { get; }
        internal ImageAspectFlags AspectFlags { get; private set; }
        
        public ulong Handle => InternalHandle?.Handle ?? 0;
        public ulong ViewHandle => _imageView?.Handle ?? 0;
        public uint UsageFlags => (uint) _imageUsageFlags;
        public ulong MemoryHandle => _imageMemory.Handle;
        public DeviceMemory DeviceMemory => _imageMemory;
        public uint MipLevels { get; private set; }
        public Vk Api { get; }
        public PixelSize Size { get; }
        public ulong MemorySize { get; private set; }
        public uint CurrentLayout => (uint) _currentLayout;

        public VulkanImage(VulkanContext vk, uint format, PixelSize size,
            bool exportable, uint mipLevels = 0)
        {
            _vk = vk;
            _instance = vk.Instance;
            _device = vk.Device;
            _physicalDevice = vk.PhysicalDevice;
            _commandBufferPool = vk.Pool;
            Format = (Format)format;
            Api = vk.Api;
            Size = size;
            MipLevels = 1;//mipLevels;
            _imageUsageFlags =
                ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit |
                ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit;
            
            //MipLevels = MipLevels != 0 ? MipLevels : (uint)Math.Floor(Math.Log(Math.Max(Size.Width, Size.Height), 2));

            var handleType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ExternalMemoryHandleTypeFlags.D3D11TextureBit :
                ExternalMemoryHandleTypeFlags.OpaqueFDBit;
            var externalMemoryCreateInfo = new ExternalMemoryImageCreateInfo
            {
                SType = StructureType.ExternalMemoryImageCreateInfo,
                HandleTypes = handleType
            };
            
            var imageCreateInfo = new ImageCreateInfo
            {
                PNext = exportable ? &externalMemoryCreateInfo : null,
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Format = Format,
                Extent =
                    new Extent3D((uint?)Size.Width,
                        (uint?)Size.Height, 1),
                MipLevels = MipLevels,
                ArrayLayers = 1,
                Samples = SampleCountFlags.Count1Bit,
                Tiling = Tiling,
                Usage = _imageUsageFlags,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = ImageCreateFlags.CreateMutableFormatBit
            };

            Api
                .CreateImage(_device, imageCreateInfo, null, out var image).ThrowOnError();
            InternalHandle = image;
            
            Api.GetImageMemoryRequirements(_device, InternalHandle.Value,
                out var memoryRequirements);


            var fdExport = new ExportMemoryAllocateInfo
            {
                HandleTypes = handleType, SType = StructureType.ExportMemoryAllocateInfo
            };
            var dedicatedAllocation = new MemoryDedicatedAllocateInfoKHR
            {
                SType = StructureType.MemoryDedicatedAllocateInfoKhr,
                Image = image
            };
            ImportMemoryWin32HandleInfoKHR handleImport = default;
            if (exportable && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                 _d3dTexture2D = D3DMemoryHelper.CreateMemoryHandle(vk.D3DDevice, size, Format);
                 using var dxgi = _d3dTexture2D.QueryInterface<SharpDX.DXGI.Resource1>();
                 
                 handleImport = new ImportMemoryWin32HandleInfoKHR
                 {
                     PNext = &dedicatedAllocation,
                     SType = StructureType.ImportMemoryWin32HandleInfoKhr,
                     HandleType = ExternalMemoryHandleTypeFlags.D3D11TextureBit,
                     Handle = dxgi.CreateSharedHandle(null, SharedResourceFlags.Read | SharedResourceFlags.Write),
                 };
            }

            var memoryAllocateInfo = new MemoryAllocateInfo
            {
                PNext =
                    exportable ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? &handleImport : &fdExport : null,
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(
                    Api,
                    _physicalDevice,
                    memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
            };

            Api.AllocateMemory(_device, memoryAllocateInfo, null,
                out var imageMemory).ThrowOnError();

            _imageMemory = imageMemory;
            
            
            MemorySize = memoryRequirements.Size;

            Api.BindImageMemory(_device, InternalHandle.Value, _imageMemory, 0).ThrowOnError();
            var componentMapping = new ComponentMapping(
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity);

            AspectFlags = ImageAspectFlags.ColorBit;

            var subresourceRange = new ImageSubresourceRange(AspectFlags, 0, MipLevels, 0, 1);

            var imageViewCreateInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = InternalHandle.Value,
                ViewType = ImageViewType.Type2D,
                Format = Format,
                Components = componentMapping,
                SubresourceRange = subresourceRange
            };

            Api
                .CreateImageView(_device, imageViewCreateInfo, null, out var imageView)
                .ThrowOnError();

            _imageView = imageView;

            _currentLayout = ImageLayout.Undefined;

            TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.NoneKhr);
        }

        public int ExportFd()
        {
            if (!Api.TryGetDeviceExtension<KhrExternalMemoryFd>(_instance, _device, out var ext))
                throw new InvalidOperationException();
            var info = new MemoryGetFdInfoKHR
            {
                Memory = _imageMemory,
                SType = StructureType.MemoryGetFDInfoKhr,
                HandleType = ExternalMemoryHandleTypeFlags.OpaqueFDBit
            };
            ext.GetMemoryF(_device, info, out var fd).ThrowOnError();
            return fd;
        }
        
        public IPlatformHandle Export()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var dxgi = _d3dTexture2D!.QueryInterface<Resource1>();
                return new PlatformHandle(
                    dxgi.CreateSharedHandle(null, SharedResourceFlags.Read | SharedResourceFlags.Write),
                    KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle);
            }
            else
                return new PlatformHandle(new IntPtr(ExportFd()),
                    KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor);
        }

        public ImageTiling Tiling => ImageTiling.Optimal;

        
        
        internal void TransitionLayout(CommandBuffer commandBuffer,
            ImageLayout fromLayout, AccessFlags fromAccessFlags,
            ImageLayout destinationLayout, AccessFlags destinationAccessFlags)
        {
            VulkanMemoryHelper.TransitionLayout(Api, commandBuffer, InternalHandle.Value,
                fromLayout,
                fromAccessFlags,
                destinationLayout, destinationAccessFlags,
                MipLevels);
            
            _currentLayout = destinationLayout;
            _currentAccessFlags = destinationAccessFlags;
        }

        internal void TransitionLayout(CommandBuffer commandBuffer,
            ImageLayout destinationLayout, AccessFlags destinationAccessFlags)
            => TransitionLayout(commandBuffer, _currentLayout, _currentAccessFlags, destinationLayout,
                destinationAccessFlags);
        
        
        internal void TransitionLayout(ImageLayout destinationLayout, AccessFlags destinationAccessFlags)
        {
            var commandBuffer = _commandBufferPool.CreateCommandBuffer();
            commandBuffer.BeginRecording();
            TransitionLayout(commandBuffer.InternalHandle, destinationLayout, destinationAccessFlags);
            commandBuffer.EndRecording();
            commandBuffer.Submit();
        }

        public void TransitionLayout(uint destinationLayout, uint destinationAccessFlags)
        {
            TransitionLayout((ImageLayout)destinationLayout, (AccessFlags)destinationAccessFlags);
        }

        public unsafe void Dispose()
        {
            Api.DestroyImageView(_device, _imageView.Value, null);
            Api.DestroyImage(_device, InternalHandle.Value, null);
            Api.FreeMemory(_device, _imageMemory, null);

            _imageView = default;
            InternalHandle = default;
            _imageMemory = default;
        }

        public void SaveTexture(string path)
        {
            _vk.GrContext.ResetContext();
            var _image = this;
            var imageInfo = new GRVkImageInfo()
            {
                CurrentQueueFamily = _vk.QueueFamilyIndex,
                Format = (uint)_image.Format,
                Image = _image.Handle,
                ImageLayout = (uint)_image.CurrentLayout,
                ImageTiling = (uint)_image.Tiling,
                ImageUsageFlags = (uint)_image.UsageFlags,
                LevelCount = _image.MipLevels,
                SampleCount = 1,
                Protected = false,
                Alloc = new GRVkAlloc()
                {
                    Memory = _image.MemoryHandle, Flags = 0, Offset = 0, Size = _image.MemorySize
                }
            };

            using (var backendTexture = new GRBackendRenderTarget(_image.Size.Width, _image.Size.Height, 1,
                       imageInfo))
            using (var surface = SKSurface.Create(_vk.GrContext, backendTexture,
                       GRSurfaceOrigin.TopLeft,
                       SKColorType.Rgba8888, SKColorSpace.CreateSrgb()))
            {
                using var snap = surface.Snapshot();
                using var encoded = snap.Encode();
                using (var s = File.Create(path))
                    encoded.SaveTo(s);
            }
        }
    }
