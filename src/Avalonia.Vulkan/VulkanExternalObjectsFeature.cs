using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal unsafe class VulkanExternalObjectsFeature : IVulkanContextExternalObjectsFeature
{
    public static string[] RequiredInstanceExtensions = {
        "VK_KHR_get_physical_device_properties2",
        "VK_KHR_external_memory_capabilities",
        "VK_KHR_external_semaphore_capabilities"
    };

    
    private static string[] s_requiredCommonDeviceExtensions =
    {
        "VK_KHR_external_memory",
        "VK_KHR_external_semaphore",
        "VK_KHR_dedicated_allocation",
    };

    private static string[] s_optionalDmaBufDeviceExtensions =
    {
        "VK_EXT_external_memory_dma_buf",
        "VK_EXT_image_drm_format_modifier",
        "VK_EXT_queue_family_foreign"
    };

    private static string[] s_requiredLinuxDeviceExtensions =
        s_requiredCommonDeviceExtensions.Concat(new[]
        {
            "VK_KHR_external_semaphore_fd",
            "VK_KHR_external_memory_fd"
        }).ToArray();

    private static string[] s_requiredWin32DeviceExtensions = s_requiredCommonDeviceExtensions.Concat(new[]
    {
        "VK_KHR_external_semaphore_win32",
        "VK_KHR_external_memory_win32"
    }).ToArray();

    public static string[] RequiredDeviceExtensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? s_requiredWin32DeviceExtensions
        : s_requiredLinuxDeviceExtensions.Concat(s_optionalDmaBufDeviceExtensions).ToArray();
    
    private readonly VulkanContext _context;
    private readonly VulkanCommandBufferPool _pool;
    private readonly bool _hasDmaBufSupport;
    private readonly bool _hasQueueFamilyForeign;

    public VulkanExternalObjectsFeature(VulkanContext context)
    {
        _context = context;

        _hasDmaBufSupport = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && context.Device.EnabledExtensions.Contains("VK_EXT_external_memory_dma_buf")
            && context.Device.EnabledExtensions.Contains("VK_EXT_image_drm_format_modifier");
        _hasQueueFamilyForeign = context.Device.EnabledExtensions.Contains("VK_EXT_queue_family_foreign");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            //TODO: keyed muted
            SupportedImageHandleTypes = new[]
            {
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle,
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueKmtHandle,
            };
            SupportedSemaphoreTypes = new[]
            {
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueNtHandle,
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueKmtHandle
            };
        }
        else
        {
            var imageHandleTypes = new List<string>
            {
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor
            };
            if (_hasDmaBufSupport)
                imageHandleTypes.Add(KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor);

            SupportedImageHandleTypes = imageHandleTypes;

            var semaphoreTypes = new List<string>
            {
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor,
            };
            semaphoreTypes.Add(KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor);

            SupportedSemaphoreTypes = semaphoreTypes;
        }

        var physicalDeviceIDProperties = new VkPhysicalDeviceIDProperties()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES
        };
        
        var physicalDeviceProperties2 = new VkPhysicalDeviceProperties2()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2,
            pNext = &physicalDeviceIDProperties
        };
        _context.InstanceApi.GetPhysicalDeviceProperties2(_context.PhysicalDeviceHandle, &physicalDeviceProperties2);

        var luid = new Span<byte>(physicalDeviceIDProperties.deviceLUID, 8).ToArray();
        if (luid.Any(b => b != 0))
            DeviceLuid = luid;
        var uuid = new Span<byte>(physicalDeviceIDProperties.deviceUUID, 16).ToArray();
        if (uuid.Any(b => b != 0))
            DeviceUuid = uuid;
        _pool = new VulkanCommandBufferPool(_context, true);
    }
    
    public IReadOnlyList<string> SupportedImageHandleTypes { get; }
    public IReadOnlyList<string> SupportedSemaphoreTypes { get; }
    public byte[]? DeviceUuid { get; }
    public byte[]? DeviceLuid { get; }
    
    
    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
    {
        if (!SupportedImageHandleTypes.Contains(imageHandleType))
            throw new ArgumentException();
        //TODO: keyed muted
        if (imageHandleType == KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor)
        {
            return CompositionGpuImportedImageSynchronizationCapabilities.Semaphores
                   | CompositionGpuImportedImageSynchronizationCapabilities.Automatic;
        }
        return CompositionGpuImportedImageSynchronizationCapabilities.Semaphores;
    }

    public IVulkanExternalImage ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        if (!SupportedImageHandleTypes.Contains(handle.HandleDescriptor))
            throw new NotSupportedException();

        if (handle.HandleDescriptor == KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor)
            return new ImportedDmaBufImage(_context, _pool, handle, properties, _hasQueueFamilyForeign);

        return new ImportedImage(_context, _pool, handle, properties);
    }

    public IVulkanExternalSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        if (!SupportedSemaphoreTypes.Contains(handle.HandleDescriptor))
            throw new NotSupportedException();

        var typeBit = handle.HandleDescriptor switch
        {
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT,
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_SYNC_FD_BIT,
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueKmtHandle =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_KMT_BIT,
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueNtHandle =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_BIT,
            _ => throw new NotSupportedException()
        };

        var semaphore = new VulkanSemaphore(_context);
        if (handle.HandleDescriptor ==
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor
            || handle.HandleDescriptor ==
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor)
        {
            var info = new VkImportSemaphoreFdInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_SEMAPHORE_FD_INFO_KHR,
                fd = handle.Handle.ToInt32(),
                handleType = typeBit,
                semaphore = semaphore.Handle,
                // SYNC_FD requires temporary import semantics — payload is consumed on first wait
                flags = handle.HandleDescriptor == KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor
                    ? VkSemaphoreImportFlags.VK_SEMAPHORE_IMPORT_TEMPORARY_BIT
                    : 0
            };
            _context.DeviceApi.ImportSemaphoreFdKHR(_context.DeviceHandle, &info)
                .ThrowOnError("vkImportSemaphoreFdKHR");
            return new ImportedSemaphore(_context, _pool, semaphore);
        }
        else
        {
            var info = new VkImportSemaphoreWin32HandleInfoKHR()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_SEMAPHORE_WIN32_HANDLE_INFO_KHR,
                handle = handle.Handle,
                handleType = typeBit,
                semaphore = semaphore.Handle
            };
            _context.DeviceApi.ImportSemaphoreWin32HandleKHR(_context.DeviceHandle, &info)
                .ThrowOnError("vkImportSemaphoreWin32HandleKHR");
            return new ImportedSemaphore(_context, _pool, semaphore);
        }
    }

    class ImportedSemaphore : IVulkanExternalSemaphore
    {
        private readonly VulkanContext _context;
        private readonly VulkanCommandBufferPool _pool;
        private VulkanSemaphore? _sem;
        private VulkanSemaphore Sem => _sem ?? throw new ObjectDisposedException(nameof(ImportedSemaphore));

        public ImportedSemaphore(VulkanContext context, VulkanCommandBufferPool pool, VulkanSemaphore sem)
        {
            _context = context;
            _pool = pool;
            _sem = sem;
        }
        
        public void Dispose()
        {
            _sem?.Dispose();
            _sem = null;
        }

        public ulong Handle => Sem.Handle.Handle;

        void SubmitSemaphore(VulkanSemaphore? wait, VulkanSemaphore? signal)
        {
            var buf = _pool.CreateCommandBuffer();
            buf.BeginRecording();
            _context.DeviceApi.CmdPipelineBarrier(
                buf.Handle,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
                0,
                0,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                0,
                null);
            
            buf.EndRecording();
            buf.Submit(wait != null ? new[] { wait }.AsSpan() : default,
                new[] { VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT },
                signal != null ? new[] { signal }.AsSpan() : default);
        }
        public void SubmitWaitSemaphore()
        {
            SubmitSemaphore(_sem, null);
        }

        public void SubmitSignalSemaphore()
        {
            SubmitSemaphore(null, _sem);
        }
    }

    class ImportedImage : VulkanImageBase, IVulkanExternalImage
    {
        private readonly IVulkanPlatformGraphicsContext _context;
        private readonly IPlatformHandle _importHandle;
        private readonly PlatformGraphicsExternalImageProperties _properties;
        private readonly VkExternalMemoryHandleTypeFlagBits _typeBit;
        public VulkanImageInfo Info => ImageInfo;

        public ImportedImage(IVulkanPlatformGraphicsContext context, VulkanCommandBufferPool commandBufferPool,
            IPlatformHandle importHandle,
            PlatformGraphicsExternalImageProperties properties) : base(context, commandBufferPool,
            properties.Format switch
            {
                PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm => VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
                PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
                _ => throw new ArgumentException($"Format {properties.Format} is not supported")
            }, new PixelSize(properties.Width, properties.Height), 1)
        {
            _context = context;
            _importHandle = importHandle;
            _properties = properties;
            _typeBit = importHandle.HandleDescriptor switch
            {
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor =>
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT,
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueKmtHandle =>
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_KMT_BIT,
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle =>
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_BIT,
                _ => throw new NotSupportedException()
            };
            var externalAlloc = new VkExternalMemoryImageCreateInfo
            {
                handleTypes = _typeBit,
                sType = VkStructureType.VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO
            };
            Initialize(&externalAlloc);
            TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT);
            this.CurrentLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
        }

        protected override VkDeviceMemory CreateMemory(VkImage image, ulong size, uint memoryTypeBits)
        {
            var handle = _importHandle;

            if (_properties.MemoryOffset != 0 || _properties.MemorySize != size)
                throw new Exception("Invalid memory size");

            var dedicated = new VkMemoryDedicatedAllocateInfo()
            {
                image = image,
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
            };

            var isPosixHandle = handle.HandleDescriptor ==
                                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor;
            var win32Info = new VkImportMemoryWin32HandleInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_MEMORY_WIN32_HANDLE_INFO_KHR,
                handle = handle.Handle,
                handleType = _typeBit,
                pNext = &dedicated
            };
            var posixInfo = new VkImportMemoryFdInfoKHR()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_MEMORY_FD_INFO_KHR,
                handleType = _typeBit,
                fd = isPosixHandle ? handle.Handle.ToInt32() : 0,
                pNext = &dedicated
            };

            var memoryAllocateInfo = new VkMemoryAllocateInfo
            {
                pNext =  new IntPtr(isPosixHandle ? &posixInfo : &win32Info),
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = size,
                memoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(_context,
                    memoryTypeBits,
                    VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT),
            };

            _context.DeviceApi.AllocateMemory(_context.DeviceHandle, ref memoryAllocateInfo, IntPtr.Zero,
                out var imageMemory).ThrowOnError("vkAllocateMemory");
            return imageMemory;
        }
    }

    /// <summary>
    /// DMA-BUF image import using VK_EXT_external_memory_dma_buf and VK_EXT_image_drm_format_modifier.
    /// This is a separate path from the opaque fd import because DMA-BUF requires modifier-aware
    /// image creation and uses different memory handle types.
    /// </summary>
    class ImportedDmaBufImage : IVulkanExternalImage
    {
        private readonly IVulkanPlatformGraphicsContext _context;
        private VkImage _image;
        private VkDeviceMemory _memory;
        private VkImageView _imageView;
        private readonly VkFormat _format;
        private readonly PixelSize _size;
        private VkImageLayout _currentLayout;

        public VulkanImageInfo Info { get; }

        public ImportedDmaBufImage(IVulkanPlatformGraphicsContext context, VulkanCommandBufferPool commandBufferPool,
            IPlatformHandle importHandle, PlatformGraphicsExternalImageProperties properties, bool hasQueueFamilyForeign)
        {
            _context = context;
            _size = new PixelSize(properties.Width, properties.Height);

            // Map DRM fourcc to VkFormat
            _format = VulkanDrmFormatMapping.TryMapDrmFourccToVkFormat(properties.DrmFourcc)
                ?? throw new ArgumentException($"Unsupported DRM fourcc: 0x{properties.DrmFourcc:X8}");

            // Step 1: Create VkImage with DRM format modifier tiling
            var planeLayout = new VkSubresourceLayout
            {
                offset = properties.MemoryOffset,
                rowPitch = properties.RowPitch,
                size = 0,
                arrayPitch = 0,
                depthPitch = 0
            };

            var drmModifierInfo = new VkImageDrmFormatModifierExplicitCreateInfoEXT
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_DRM_FORMAT_MODIFIER_EXPLICIT_CREATE_INFO_EXT,
                drmFormatModifier = properties.DrmModifier,
                drmFormatModifierPlaneCount = 1,
                pPlaneLayouts = &planeLayout,
            };

            var externalMemoryInfo = new VkExternalMemoryImageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO,
                handleTypes = VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT,
                pNext = &drmModifierInfo
            };

            var createInfo = new VkImageCreateInfo
            {
                pNext = &externalMemoryInfo,
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
                imageType = VkImageType.VK_IMAGE_TYPE_2D,
                format = _format,
                extent = new VkExtent3D
                {
                    width = (uint)properties.Width,
                    height = (uint)properties.Height,
                    depth = 1
                },
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                tiling = VkImageTiling.VK_IMAGE_TILING_DRM_FORMAT_MODIFIER_EXT,
                usage = VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT
                        | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            };

            context.DeviceApi.CreateImage(context.DeviceHandle, ref createInfo, IntPtr.Zero, out _image)
                .ThrowOnError("vkCreateImage (DMA-BUF)");

            try
            {
                // Step 2: Query memory requirements
                context.DeviceApi.GetImageMemoryRequirements(context.DeviceHandle, _image, out var memReqs);

                // Step 3: Query memory type bits from the DMA-BUF fd
                var fdProperties = new VkMemoryFdPropertiesKHR
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_FD_PROPERTIES_KHR
                };
                context.DeviceApi.GetMemoryFdPropertiesKHR(context.DeviceHandle,
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT,
                    importHandle.Handle.ToInt32(), &fdProperties).ThrowOnError("vkGetMemoryFdPropertiesKHR");

                var combinedMemoryTypeBits = memReqs.memoryTypeBits & fdProperties.memoryTypeBits;
                if (combinedMemoryTypeBits == 0)
                    combinedMemoryTypeBits = fdProperties.memoryTypeBits;

                // Step 4: Import memory
                var dedicated = new VkMemoryDedicatedAllocateInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
                    image = _image,
                };

                var importInfo = new VkImportMemoryFdInfoKHR
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_MEMORY_FD_INFO_KHR,
                    handleType = VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT,
                    fd = importHandle.Handle.ToInt32(),
                    pNext = &dedicated,
                };

                var memoryAllocateInfo = new VkMemoryAllocateInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                    pNext = new IntPtr(&importInfo),
                    allocationSize = memReqs.size,
                    memoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(context,
                        combinedMemoryTypeBits,
                        VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT),
                };

                context.DeviceApi.AllocateMemory(context.DeviceHandle, ref memoryAllocateInfo, IntPtr.Zero,
                    out _memory).ThrowOnError("vkAllocateMemory (DMA-BUF)");

                // Step 5: Bind memory
                context.DeviceApi.BindImageMemory(context.DeviceHandle, _image, _memory, 0)
                    .ThrowOnError("vkBindImageMemory (DMA-BUF)");

                // Step 6: Transition layout with foreign queue ownership transfer
                var barrier = new VkImageMemoryBarrier
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
                    srcAccessMask = 0,
                    dstAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT,
                    oldLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                    newLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                    srcQueueFamilyIndex = hasQueueFamilyForeign
                        ? VkQueueFamilyConstants.VK_QUEUE_FAMILY_FOREIGN_EXT
                        : VkQueueFamilyConstants.VK_QUEUE_FAMILY_EXTERNAL,
                    dstQueueFamilyIndex = context.GraphicsQueueFamilyIndex,
                    image = _image,
                    subresourceRange = new VkImageSubresourceRange
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                        baseMipLevel = 0,
                        levelCount = 1,
                        baseArrayLayer = 0,
                        layerCount = 1,
                    }
                };

                var cmdBuf = commandBufferPool.CreateCommandBuffer();
                cmdBuf.BeginRecording();
                context.DeviceApi.CmdPipelineBarrier(
                    cmdBuf.Handle,
                    VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,
                    VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
                    0, 0, IntPtr.Zero, 0, IntPtr.Zero,
                    1, &barrier);
                cmdBuf.EndRecording();
                cmdBuf.Submit();

                _currentLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;

                // Step 7: Create image view
                var viewCreateInfo = new VkImageViewCreateInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                    image = _image,
                    viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
                    format = _format,
                    components = new VkComponentMapping(),
                    subresourceRange = new VkImageSubresourceRange
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                        baseMipLevel = 0,
                        levelCount = 1,
                        baseArrayLayer = 0,
                        layerCount = 1,
                    }
                };

                context.DeviceApi.CreateImageView(context.DeviceHandle, ref viewCreateInfo, IntPtr.Zero, out _imageView)
                    .ThrowOnError("vkCreateImageView (DMA-BUF)");

                Info = new VulkanImageInfo
                {
                    Handle = _image.Handle,
                    PixelSize = _size,
                    Format = (uint)_format,
                    MemoryHandle = _memory.Handle,
                    MemorySize = memReqs.size,
                    ViewHandle = _imageView.Handle,
                    UsageFlags = (uint)(VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT),
                    Layout = (uint)_currentLayout,
                    Tiling = (uint)VkImageTiling.VK_IMAGE_TILING_DRM_FORMAT_MODIFIER_EXT,
                    LevelCount = 1,
                    SampleCount = 1,
                    IsProtected = false
                };
            }
            catch
            {
                if (_imageView.Handle != 0)
                    context.DeviceApi.DestroyImageView(context.DeviceHandle, _imageView, IntPtr.Zero);
                if (_memory.Handle != 0)
                    context.DeviceApi.FreeMemory(context.DeviceHandle, _memory, IntPtr.Zero);
                if (_image.Handle != 0)
                    context.DeviceApi.DestroyImage(context.DeviceHandle, _image, IntPtr.Zero);
                throw;
            }
        }

        public void Dispose()
        {
            var api = _context.DeviceApi;
            var d = _context.DeviceHandle;
            if (_imageView.Handle != 0)
            {
                api.DestroyImageView(d, _imageView, IntPtr.Zero);
                _imageView = default;
            }
            if (_image.Handle != 0)
            {
                api.DestroyImage(d, _image, IntPtr.Zero);
                _image = default;
            }
            if (_memory.Handle != 0)
            {
                api.FreeMemory(d, _memory, IntPtr.Zero);
                _memory = default;
            }
        }
    }
}