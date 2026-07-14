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
        private readonly Instance _instance;
        private readonly Device _device;
        private readonly PhysicalDevice _physicalDevice;
        private readonly VulkanCommandBufferPool _commandBufferPool;
        private ImageLayout _currentLayout;
        private AccessFlags _currentAccessFlags;
        private ImageUsageFlags _imageUsageFlags { get; }
        private ImageView _imageView { get; set; }
        private DeviceMemory _imageMemory { get; set; }
        private ComPtr<ID3D11Texture2D> _d3dTexture2D;
        
        internal Image InternalHandle { get; private set; }
        internal Format Format { get; }
        internal ImageAspectFlags AspectFlags { get; }
        
        public ulong Handle => InternalHandle.Handle;
        public ulong ViewHandle => _imageView.Handle;
        public uint UsageFlags => (uint) _imageUsageFlags;
        public ulong MemoryHandle => _imageMemory.Handle;
        public DeviceMemory DeviceMemory => _imageMemory;
        public uint MipLevels { get; }
        public Vk Api { get; }
        public PixelSize Size { get; }
        public ulong MemorySize { get; private set; }
        public uint CurrentLayout => (uint) _currentLayout;

        private bool _hasIOSurface;
        private readonly bool _dmaBuf;

        /// <summary>The DRM fourcc format of the dma-buf backing, valid only for dma-buf images.</summary>
        public uint DrmFormat { get; private set; }

        /// <summary>The DRM format modifier chosen by the driver, valid only for dma-buf images.</summary>
        public ulong DrmModifier { get; private set; }

        /// <summary>Per-plane byte offsets into the dma-buf, valid only for dma-buf images.</summary>
        public ulong[]? PlaneOffsets { get; private set; }

        /// <summary>Per-plane row strides of the dma-buf, valid only for dma-buf images.</summary>
        public ulong[]? PlaneStrides { get; private set; }

        public VulkanImage(VulkanContext vk, uint format, PixelSize size,
            bool exportable, IReadOnlyList<string> supportedHandleTypes, bool dmaBuf = false)
        {
            _dmaBuf = dmaBuf;
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

            if (_dmaBuf)
            {
                CreateDmaBufImage();
            }
            else
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
                Usage = _imageUsageFlags,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = ImageCreateFlags.CreateMutableFormatBit
            };

            Api
                .CreateImage(_device, in imageCreateInfo, null, out var image).ThrowOnError();
            InternalHandle = image;

            if (!exportable || !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

                Api.GetImageMemoryRequirements(_device, InternalHandle,
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
                        _physicalDevice,
                        memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
                };

                Api.AllocateMemory(_device, in memoryAllocateInfo, null,
                    out var imageMemory).ThrowOnError();

                _imageMemory = imageMemory;


                MemorySize = memoryRequirements.Size;

                Api.BindImageMemory(_device, InternalHandle, _imageMemory, 0).ThrowOnError();
            }
            }

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
                Image = InternalHandle,
                ViewType = ImageViewType.Type2D,
                Format = Format,
                Components = componentMapping,
                SubresourceRange = subresourceRange
            };

            Api
                .CreateImageView(_device, in imageViewCreateInfo, null, out var imageView)
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
            if (!Api.TryGetDeviceExtension<KhrExternalMemoryFd>(_instance, _device, out var ext))
                throw new InvalidOperationException();
            var info = new MemoryGetFdInfoKHR
            {
                Memory = _imageMemory,
                SType = StructureType.MemoryGetFDInfoKhr,
                HandleType = ExternalMemoryHandleTypeFlags.OpaqueFDBit
            };
            ext.GetMemoryF(_device, in info, out var fd).ThrowOnError();
            return fd;
        }

        /// <summary>
        /// Creates an image whose memory is laid out according to a DRM format modifier, allocates
        /// dma-buf-exportable dedicated memory for it and records the resulting modifier and per-plane
        /// layout so the image can be handed to <see cref="KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor"/>
        /// consumers such as <c>EglExternalObjectsFeature</c>.
        /// </summary>
        private void CreateDmaBufImage()
        {
            // DRM packed 32-bit formats encode the bit depth as the '2','4' digits, not literal channel
            // letters: e.g. DRM_FORMAT_ARGB8888 == fourcc('A','R','2','4') == 0x34325241.
            DrmFormat = Format switch
            {
                Format.B8G8R8A8Unorm => DrmFourcc('A', 'R', '2', '4'), // DRM_FORMAT_ARGB8888
                Format.R8G8B8A8Unorm => DrmFourcc('A', 'B', '2', '4'), // DRM_FORMAT_ABGR8888
                _ => throw new NotSupportedException($"Format {Format} has no known DRM fourcc mapping")
            };

            if (!Api.TryGetDeviceExtension<ExtImageDrmFormatModifier>(_instance, _device, out var drmExt))
                throw new InvalidOperationException("VK_EXT_image_drm_format_modifier is not available");

            // Query which DRM modifiers the driver supports for this format and pick the ones that
            // can be used both as a render target and as a blit destination.
            var modifierList = new DrmFormatModifierPropertiesListEXT
            {
                SType = StructureType.DrmFormatModifierPropertiesListExt
            };
            var formatProperties = new FormatProperties2
            {
                SType = StructureType.FormatProperties2,
                PNext = &modifierList
            };
            Api.GetPhysicalDeviceFormatProperties2(_physicalDevice, Format, &formatProperties);

            var modifierProps = new DrmFormatModifierPropertiesEXT[modifierList.DrmFormatModifierCount];
            fixed (DrmFormatModifierPropertiesEXT* pModifierProps = modifierProps)
            {
                modifierList.PDrmFormatModifierProperties = pModifierProps;
                Api.GetPhysicalDeviceFormatProperties2(_physicalDevice, Format, &formatProperties);
            }

            const FormatFeatureFlags requiredFeatures =
                FormatFeatureFlags.ColorAttachmentBit | FormatFeatureFlags.TransferDstBit;
            // Only consider single-memory-plane layouts: multi-plane modifiers (e.g. AMD DCC) carry
            // compression metadata that EGL_EXT_image_dma_buf_import implementations generally refuse
            // to import as a sampleable texture.
            var singlePlaneModifiers = modifierProps
                .Where(p => (p.DrmFormatModifierTilingFeatures & requiredFeatures) == requiredFeatures
                            && p.DrmFormatModifierPlaneCount == 1)
                .Select(p => p.DrmFormatModifier)
                .ToArray();
            if (singlePlaneModifiers.Length == 0)
                throw new NotSupportedException("No single-plane DRM format modifier supports rendering to this format");

            // DRM_FORMAT_MOD_LINEAR is importable everywhere, so prefer it when the driver offers it.
            const ulong drmFormatModLinear = 0;
            var candidateModifiers = singlePlaneModifiers.Contains(drmFormatModLinear)
                ? new[] { drmFormatModLinear }
                : singlePlaneModifiers;

            Image image;
            fixed (ulong* pModifiers = candidateModifiers)
            {
                var modifierListCreateInfo = new ImageDrmFormatModifierListCreateInfoEXT
                {
                    SType = StructureType.ImageDrmFormatModifierListCreateInfoExt,
                    DrmFormatModifierCount = (uint)candidateModifiers.Length,
                    PDrmFormatModifiers = pModifiers
                };
                var externalMemoryCreateInfo = new ExternalMemoryImageCreateInfo
                {
                    SType = StructureType.ExternalMemoryImageCreateInfo,
                    HandleTypes = ExternalMemoryHandleTypeFlags.DmaBufBitExt,
                    PNext = &modifierListCreateInfo
                };
                var imageCreateInfo = new ImageCreateInfo
                {
                    PNext = &externalMemoryCreateInfo,
                    SType = StructureType.ImageCreateInfo,
                    ImageType = ImageType.Type2D,
                    Format = Format,
                    Extent = new Extent3D((uint)Size.Width, (uint)Size.Height, 1),
                    MipLevels = MipLevels,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.Count1Bit,
                    Tiling = ImageTiling.DrmFormatModifierExt,
                    Usage = _imageUsageFlags,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined
                };
                Api.CreateImage(_device, in imageCreateInfo, null, out image).ThrowOnError();
            }
            InternalHandle = image;

            // Find out which modifier the driver actually chose and how many memory planes it has.
            var imageModifierProps = new ImageDrmFormatModifierPropertiesEXT
            {
                SType = StructureType.ImageDrmFormatModifierPropertiesExt
            };
            drmExt.GetImageDrmFormatModifierProperties(_device, image, &imageModifierProps).ThrowOnError();
            DrmModifier = imageModifierProps.DrmFormatModifier;

            var planeCount = 1;
            foreach (var p in modifierProps)
                if (p.DrmFormatModifier == DrmModifier)
                    planeCount = (int)p.DrmFormatModifierPlaneCount;

            Api.GetImageMemoryRequirements(_device, InternalHandle, out var memoryRequirements);

            var dedicatedAllocation = new MemoryDedicatedAllocateInfoKHR
            {
                SType = StructureType.MemoryDedicatedAllocateInfoKhr, Image = image
            };
            var exportAllocateInfo = new ExportMemoryAllocateInfo
            {
                SType = StructureType.ExportMemoryAllocateInfo,
                HandleTypes = ExternalMemoryHandleTypeFlags.DmaBufBitExt,
                PNext = &dedicatedAllocation
            };
            var memoryAllocateInfo = new MemoryAllocateInfo
            {
                PNext = &exportAllocateInfo,
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(
                    Api, _physicalDevice, memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
            };
            Api.AllocateMemory(_device, in memoryAllocateInfo, null, out var imageMemory).ThrowOnError();
            _imageMemory = imageMemory;
            MemorySize = memoryRequirements.Size;
            Api.BindImageMemory(_device, InternalHandle, _imageMemory, 0).ThrowOnError();

            // Query the per-plane offset/stride that the consumer needs to interpret the dma-buf.
            PlaneOffsets = new ulong[planeCount];
            PlaneStrides = new ulong[planeCount];
            var planeAspects = new[]
            {
                ImageAspectFlags.MemoryPlane0BitExt, ImageAspectFlags.MemoryPlane1BitExt,
                ImageAspectFlags.MemoryPlane2BitExt, ImageAspectFlags.MemoryPlane3BitExt
            };
            for (var plane = 0; plane < planeCount; plane++)
            {
                var subresource = new ImageSubresource
                {
                    AspectMask = planeAspects[plane], MipLevel = 0, ArrayLayer = 0
                };
                Api.GetImageSubresourceLayout(_device, InternalHandle, in subresource, out var layout);
                PlaneOffsets[plane] = layout.Offset;
                PlaneStrides[plane] = layout.RowPitch;
            }
        }

        private int ExportDmaBufFd()
        {
            if (!Api.TryGetDeviceExtension<KhrExternalMemoryFd>(_instance, _device, out var ext))
                throw new InvalidOperationException();
            var info = new MemoryGetFdInfoKHR
            {
                Memory = _imageMemory,
                SType = StructureType.MemoryGetFDInfoKhr,
                HandleType = ExternalMemoryHandleTypeFlags.DmaBufBitExt
            };
            ext.GetMemoryF(_device, in info, out var fd).ThrowOnError();
            return fd;
        }

        /// <summary>
        /// Exports the dma-buf and the metadata required to import it into an EGL/OpenGL consumer.
        /// The returned file descriptors are owned by the caller and must be closed once the import completes.
        /// </summary>
        public (IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties, int[] fds) ExportDmaBuf()
        {
            if (!_dmaBuf || PlaneOffsets == null || PlaneStrides == null)
                throw new InvalidOperationException("Image is not dma-buf backed");

            var planeCount = PlaneOffsets.Length;
            var fd = ExportDmaBufFd();

            // EGL wants a file descriptor for every plane; all planes of a single allocation share the
            // same memory, so we hand out duplicates of the same fd and let the importer dup them again.
            var fds = new int[planeCount];
            fds[0] = fd;
            for (var plane = 1; plane < planeCount; plane++)
                fds[plane] = LibcDup(fd);

            var planeOffsets = new uint[planeCount];
            var planeStrides = new uint[planeCount];
            for (var plane = 0; plane < planeCount; plane++)
            {
                planeOffsets[plane] = (uint)PlaneOffsets[plane];
                planeStrides[plane] = (uint)PlaneStrides[plane];
            }

            var properties = new PlatformGraphicsExternalImageProperties
            {
                Width = Size.Width,
                Height = Size.Height,
                Format = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
                    : PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
                MemorySize = MemorySize,
                DrmFormat = DrmFormat,
                DrmModifier = DrmModifier,
                PlaneCount = planeCount,
                PlaneFds = fds,
                PlaneOffsets = planeOffsets,
                PlaneStrides = planeStrides
            };

            return (new PlatformHandle(new IntPtr(fd),
                KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor), properties, fds);
        }

        private static uint DrmFourcc(char a, char b, char c, char d) =>
            (uint)a | ((uint)b << 8) | ((uint)c << 16) | ((uint)d << 24);

        [DllImport("libc", EntryPoint = "dup")]
        private static extern int LibcDup(int fd);

        public IntPtr ExportOpaqueNtHandle()
        {
            if (!Api.TryGetDeviceExtension<KhrExternalMemoryWin32>(_instance, _device, out var ext))
                throw new InvalidOperationException();
            var info = new MemoryGetWin32HandleInfoKHR()
            {
                Memory = _imageMemory,
                SType = StructureType.MemoryGetWin32HandleInfoKhr,
                HandleType = ExternalMemoryHandleTypeFlags.OpaqueWin32Bit
            };
            ext.GetMemoryWin32Handle(_device, in info, out var fd).ThrowOnError();
            return fd;
        }

        public IntPtr ExportIOSurface()
        {
            if (!Api.TryGetDeviceExtension<ExtMetalObjects>(_instance, _device, out var ext))
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
            ext.ExportMetalObjects(_device, ref export);
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

        public ImageTiling Tiling => _dmaBuf ? ImageTiling.DrmFormatModifierExt : ImageTiling.Optimal;

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
            Api.DestroyImageView(_device, _imageView, null);
            Api.DestroyImage(_device, InternalHandle, null);
            Api.FreeMemory(_device, _imageMemory, null);

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
