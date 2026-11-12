using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Vulkan;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using SilkNetDemo;
using SkiaSharp;
using static Silk.NET.Core.Native.SilkMarshal;
using Device = Silk.NET.Vulkan.Device;
using Format = Silk.NET.Vulkan.Format;

namespace GpuInterop.VulkanDemo;

public unsafe class VulkanImage : IDisposable
    {
        private readonly VulkanContext _vk;
        private readonly VulkanCommandBufferPool _commandBufferPool;
        private ImageLayout _currentLayout;
        private AccessFlags _currentAccessFlags;
        private ImageView _imageView { get; set; }
        private DeviceMemory _imageMemory { get; set; }
        private ComPtr<ID3D11Texture2D> _d3dTexture2D;

        protected Instance Instance { get; }
        protected Device Device { get; }
        protected PhysicalDevice PhysicalDevice { get; }
        protected ImageUsageFlags ImageUsageFlags { get; }

        internal Image InternalHandle { get; private protected set; }
        internal Format Format { get; }
        internal ImageAspectFlags AspectFlags { get; }
        
        public ulong Handle => InternalHandle.Handle;
        public ulong ViewHandle => _imageView.Handle;
        public uint UsageFlags => (uint) ImageUsageFlags;
        public ulong MemoryHandle => _imageMemory.Handle;
        public DeviceMemory DeviceMemory => _imageMemory;
        public uint MipLevels { get; }
        public Vk Api { get; }
        public PixelSize Size { get; }
        public ulong MemorySize { get; private set; }
        public uint CurrentLayout => (uint) _currentLayout;

        private bool _hasIOSurface;
        
        protected VulkanImage(VulkanContext vk, uint format, PixelSize size)
        {
            _vk = vk;
            Instance = vk.Instance;
            Device = vk.Device;
            PhysicalDevice = vk.PhysicalDevice;
            _commandBufferPool = vk.Pool;
            Format = (Format)format;
            Api = vk.Api;
            Size = size;
            MipLevels = 1;//mipLevels;
            ImageUsageFlags =
                ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit |
                ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit;
            AspectFlags = ImageAspectFlags.ColorBit;

            //MipLevels = MipLevels != 0 ? MipLevels : (uint)Math.Floor(Math.Log(Math.Max(Size.Width, Size.Height), 2));
        }

        public VulkanImage(VulkanContext vk, uint format, PixelSize size,
            bool exportable, IReadOnlyList<string> supportedHandleTypes) : this(vk, format, size)
        {
            var handleType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                (supportedHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle)
                 && !supportedHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle) ?
                    ExternalMemoryHandleTypeFlags.D3D11TextureBit :
                    ExternalMemoryHandleTypeFlags.OpaqueWin32Bit) :
                ExternalMemoryHandleTypeFlags.OpaqueFDBit;

            var externalMemoryCreateInfo = new ExternalMemoryImageCreateInfo
            {
                SType = StructureType.ExternalMemoryImageCreateInfo,
                HandleTypes = handleType
            };


            var ioSurfaceCreateInfo = new ExportMetalObjectCreateInfoEXT
            {
                SType = StructureType.ExportMetalObjectCreateInfoExt,
                ExportObjectType = ExportMetalObjectTypeFlagsEXT.IosurfaceBitExt
            };

            _hasIOSurface = exportable && RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            var imageCreateInfo = new ImageCreateInfo
            {
                PNext = exportable ?
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
                        &ioSurfaceCreateInfo :
                        &externalMemoryCreateInfo : null,
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
                Usage = ImageUsageFlags,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = ImageCreateFlags.CreateMutableFormatBit
            };

            Api
                .CreateImage(Device, in imageCreateInfo, null, out var image).ThrowOnError();
            InternalHandle = image;

            if (!exportable || !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

                Api.GetImageMemoryRequirements(Device, InternalHandle,
                    out var memoryRequirements);

                var dedicatedAllocation = new MemoryDedicatedAllocateInfoKHR
                {
                    SType = StructureType.MemoryDedicatedAllocateInfoKhr, Image = image
                };

                var fdExport = new ExportMemoryAllocateInfo
                {
                    HandleTypes = handleType,
                    SType = StructureType.ExportMemoryAllocateInfo,
                    PNext = &dedicatedAllocation
                };

                ImportMemoryWin32HandleInfoKHR handleImport = default;
                if (handleType == ExternalMemoryHandleTypeFlags.D3D11TextureBit && exportable)
                {
                    if (vk.D3DDevice.Handle == null)
                        throw new NotSupportedException("Vulkan D3DDevice wasn't created");
                    _d3dTexture2D = D3DMemoryHelper.CreateMemoryHandle(vk.D3DDevice, size, Format);

                    handleImport = new ImportMemoryWin32HandleInfoKHR
                    {
                        PNext = &dedicatedAllocation,
                        SType = StructureType.ImportMemoryWin32HandleInfoKhr,
                        HandleType = ExternalMemoryHandleTypeFlags.D3D11TextureBit,
                        Handle = CreateDxgiSharedHandle()
                    };
                }

                var memoryAllocateInfo = new MemoryAllocateInfo
                {
                    PNext =
                        exportable ? handleImport.Handle != IntPtr.Zero ? &handleImport : &fdExport : null,
                    SType = StructureType.MemoryAllocateInfo,
                    AllocationSize = memoryRequirements.Size,
                    MemoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(
                        Api,
                        PhysicalDevice,
                        memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
                };

                Api.AllocateMemory(Device, in memoryAllocateInfo, null,
                    out var imageMemory).ThrowOnError();

                AttachMemory(imageMemory, memoryRequirements.Size);
            }

            CreateImageView();
        }

        /// <summary>
        /// Records <paramref name="memory"/> as the backing of <see cref="InternalHandle"/> and binds it.
        /// </summary>
        protected void AttachMemory(DeviceMemory memory, ulong size)
        {
            _imageMemory = memory;
            MemorySize = size;
            Api.BindImageMemory(Device, InternalHandle, _imageMemory, 0).ThrowOnError();
        }

        /// <summary>
        /// Creates the image view over <see cref="InternalHandle"/> and moves the image into its initial layout.
        /// Must be called once by every constructor after the image and its memory exist.
        /// </summary>
        protected void CreateImageView()
        {
            var componentMapping = new ComponentMapping(
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity);

            var subresourceRange = new ImageSubresourceRange(AspectFlags, 0, MipLevels, 0, 1);

            var imageViewCreateInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = InternalHandle,
                ViewType = ImageViewType.Type2D,
                Format = Format,
                Components = componentMapping,
                SubresourceRange = subresourceRange
            };

            Api
                .CreateImageView(Device, in imageViewCreateInfo, null, out var imageView)
                .ThrowOnError();

            _imageView = imageView;

            _currentLayout = ImageLayout.Undefined;

            TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.NoneKhr);
        }

        private IntPtr CreateDxgiSharedHandle()
        {
            using var dxgiResource = _d3dTexture2D.QueryInterface<IDXGIResource1>();

            void* sharedHandle;
            ThrowHResult(dxgiResource.CreateSharedHandle(
                (SecurityAttributes*) null,
                DXGI.SharedResourceRead | DXGI.SharedResourceWrite,
                (char*)null,
                &sharedHandle));

            return (IntPtr)sharedHandle;
        }

        public int ExportFd()
        {
            if (!Api.TryGetDeviceExtension<KhrExternalMemoryFd>(Instance, Device, out var ext))
                throw new InvalidOperationException();
            var info = new MemoryGetFdInfoKHR
            {
                Memory = _imageMemory,
                SType = StructureType.MemoryGetFDInfoKhr,
                HandleType = ExternalMemoryHandleTypeFlags.OpaqueFDBit
            };
            ext.GetMemoryF(Device, in info, out var fd).ThrowOnError();
            return fd;
        }

        public IntPtr ExportOpaqueNtHandle()
        {
            if (!Api.TryGetDeviceExtension<KhrExternalMemoryWin32>(Instance, Device, out var ext))
                throw new InvalidOperationException();
            var info = new MemoryGetWin32HandleInfoKHR()
            {
                Memory = _imageMemory,
                SType = StructureType.MemoryGetWin32HandleInfoKhr,
                HandleType = ExternalMemoryHandleTypeFlags.OpaqueWin32Bit
            };
            ext.GetMemoryWin32Handle(Device, in info, out var fd).ThrowOnError();
            return fd;
        }

        public IntPtr ExportIOSurface()
        {
            if (!Api.TryGetDeviceExtension<ExtMetalObjects>(Instance, Device, out var ext))
                throw new InvalidOperationException();
            var surfaceExport = new ExportMetalIOSurfaceInfoEXT
            {
                SType = StructureType.ExportMetalIOSurfaceInfoExt,
                Image = InternalHandle
            };
            var export = new ExportMetalObjectsInfoEXT()
            {
                SType = StructureType.ExportMetalObjectsInfoExt,
                PNext = &surfaceExport
            };
            ext.ExportMetalObjects(Device, ref export);
            if (surfaceExport.IoSurface == IntPtr.Zero)
                throw new Exception("Unable to export IOSurfaceRef");
            return surfaceExport.IoSurface;
        }
        
        public IPlatformHandle Export()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_d3dTexture2D.Handle != null)
                {
                    return new PlatformHandle(
                        CreateDxgiSharedHandle(),
                        KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle);
                }

                return new PlatformHandle(ExportOpaqueNtHandle(),
                    KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new PlatformHandle(ExportIOSurface(),
                    KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef);
            else
                return new PlatformHandle(new IntPtr(ExportFd()),
                    KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor);
        }

        public virtual ImageTiling Tiling => ImageTiling.Optimal;

        public bool IsDirectXBacked => _d3dTexture2D.Handle != null;
        
        internal void TransitionLayout(CommandBuffer commandBuffer,
            ImageLayout fromLayout, AccessFlags fromAccessFlags,
            ImageLayout destinationLayout, AccessFlags destinationAccessFlags)
        {
            VulkanMemoryHelper.TransitionLayout(Api, commandBuffer, InternalHandle,
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
            Api.DestroyImageView(Device, _imageView, null);
            Api.DestroyImage(Device, InternalHandle, null);
            Api.FreeMemory(Device, _imageMemory, null);

            _imageView = default;
            InternalHandle = default;
            _imageMemory = default;
        }

        public void SaveTexture(string path)
        {
            if (_vk.GrContext == null)
            {
                if (_hasIOSurface)
                {
                    var surf = ExportIOSurface();
                    if (NativeMethods.IOSurfaceLock(surf, 0, IntPtr.Zero) != 0)
                        throw new Exception("IOSurfaceLock failed");
                    var w = (int)NativeMethods.IOSurfaceGetWidth(surf);
                    var h = (int)NativeMethods.IOSurfaceGetHeight(surf);
                    var sstride = NativeMethods.IOSurfaceGetBytesPerRow(surf);

                    var pSurface = NativeMethods.IOSurfaceGetBaseAddress(surf);
                    using var b = new Avalonia.Media.Imaging.Bitmap(PixelFormat.Bgra8888,
                        AlphaFormat.Premul, pSurface, new PixelSize(w, h),
                        new Vector(96, 96), (int)sstride);
                    b.Save(path, PngBitmapEncoderOptions.Default);

                    NativeMethods.IOSurfaceUnlock(surf, 0, IntPtr.Zero);
                    return;
                }
                else
                    throw new NotSupportedException("Need skia to dump textures, sorry");
            }

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

            using (var backendTexture = new GRBackendRenderTarget(_image.Size.Width, _image.Size.Height, imageInfo))
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
