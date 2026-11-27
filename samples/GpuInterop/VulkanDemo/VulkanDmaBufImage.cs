using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using SilkNetDemo;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Format = Silk.NET.Vulkan.Format;

namespace GpuInterop.VulkanDemo;

public unsafe class VulkanDmaBufImage : VulkanImage
{
    /// <summary>The DRM fourcc format of the dma-buf backing.</summary>
    public uint DrmFormat { get; }

    /// <summary>The DRM format modifier chosen by the driver.</summary>
    public ulong DrmModifier { get; }

    /// <summary>Per-plane byte offsets into the dma-buf.</summary>
    public ulong[] PlaneOffsets { get; }

    /// <summary>Per-plane row strides of the dma-buf.</summary>
    public ulong[] PlaneStrides { get; }

    public override ImageTiling Tiling => ImageTiling.DrmFormatModifierExt;

    public VulkanDmaBufImage(VulkanContext vk, uint format, PixelSize size) : base(vk, format, size)
    {
        DrmFormat = Format switch
        {
            Format.B8G8R8A8Unorm => DrmFourcc('A', 'R', '2', '4'), // DRM_FORMAT_ARGB8888
            Format.R8G8B8A8Unorm => DrmFourcc('A', 'B', '2', '4'), // DRM_FORMAT_ABGR8888
            _ => throw new NotSupportedException($"Format {Format} has no known DRM fourcc mapping")
        };

        if (!Api.TryGetDeviceExtension<ExtImageDrmFormatModifier>(Instance, Device, out var drmExt))
            throw new InvalidOperationException("VK_EXT_image_drm_format_modifier is not available");

        var modifierList = new DrmFormatModifierPropertiesListEXT
        {
            SType = StructureType.DrmFormatModifierPropertiesListExt
        };
        var formatProperties = new FormatProperties2
        {
            SType = StructureType.FormatProperties2,
            PNext = &modifierList
        };
        Api.GetPhysicalDeviceFormatProperties2(PhysicalDevice, Format, &formatProperties);

        var modifierProps = new DrmFormatModifierPropertiesEXT[modifierList.DrmFormatModifierCount];
        fixed (DrmFormatModifierPropertiesEXT* pModifierProps = modifierProps)
        {
            modifierList.PDrmFormatModifierProperties = pModifierProps;
            Api.GetPhysicalDeviceFormatProperties2(PhysicalDevice, Format, &formatProperties);
        }

        const FormatFeatureFlags requiredFeatures =
            FormatFeatureFlags.ColorAttachmentBit | FormatFeatureFlags.TransferDstBit;

        var singlePlaneModifiers = modifierProps
            .Where(p => (p.DrmFormatModifierTilingFeatures & requiredFeatures) == requiredFeatures
                        && p.DrmFormatModifierPlaneCount == 1)
            .Select(p => p.DrmFormatModifier)
            .ToArray();
        if (singlePlaneModifiers.Length == 0)
            throw new NotSupportedException("No single-plane DRM format modifier supports rendering to this format");

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
                Tiling = Tiling,
                Usage = ImageUsageFlags,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined
            };
            Api.CreateImage(Device, in imageCreateInfo, null, out image).ThrowOnError();
        }
        InternalHandle = image;

        var imageModifierProps = new ImageDrmFormatModifierPropertiesEXT
        {
            SType = StructureType.ImageDrmFormatModifierPropertiesExt
        };
        drmExt.GetImageDrmFormatModifierProperties(Device, image, &imageModifierProps).ThrowOnError();
        DrmModifier = imageModifierProps.DrmFormatModifier;

        var planeCount = 1;
        foreach (var p in modifierProps)
            if (p.DrmFormatModifier == DrmModifier)
                planeCount = (int)p.DrmFormatModifierPlaneCount;

        Api.GetImageMemoryRequirements(Device, InternalHandle, out var memoryRequirements);

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
                Api, PhysicalDevice, memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };
        Api.AllocateMemory(Device, in memoryAllocateInfo, null, out var imageMemory).ThrowOnError();
        AttachMemory(imageMemory, memoryRequirements.Size);

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
            Api.GetImageSubresourceLayout(Device, InternalHandle, in subresource, out var layout);
            PlaneOffsets[plane] = layout.Offset;
            PlaneStrides[plane] = layout.RowPitch;
        }

        CreateImageView();
    }

    private int ExportDmaBufFd()
    {
        if (!Api.TryGetDeviceExtension<KhrExternalMemoryFd>(Instance, Device, out var ext))
            throw new InvalidOperationException();
        var info = new MemoryGetFdInfoKHR
        {
            Memory = DeviceMemory,
            SType = StructureType.MemoryGetFDInfoKhr,
            HandleType = ExternalMemoryHandleTypeFlags.DmaBufBitExt
        };
        ext.GetMemoryF(Device, in info, out var fd).ThrowOnError();
        return fd;
    }

    public (IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties, int[] fds) ExportDmaBuf()
    {
        var planeCount = PlaneOffsets.Length;
        var fd = ExportDmaBufFd();

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
            Format = Format == Format.B8G8R8A8Unorm
                ? PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
                : PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
            MemorySize = MemorySize,
            DmaBufProperties = new PlatformGraphicsExternalImageDmaBufProperties
            {
                DrmFormat = DrmFormat,
                DrmModifier = DrmModifier,
                PlaneCount = planeCount,
                PlaneFds = fds,
                PlaneOffsets = planeOffsets,
                PlaneStrides = planeStrides
            }
        };

        return (new PlatformHandle(new IntPtr(fd),
            KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor), properties, fds);
    }

    private static uint DrmFourcc(char a, char b, char c, char d) =>
        (uint)a | ((uint)b << 8) | ((uint)c << 16) | ((uint)d << 24);

    [DllImport("libc", EntryPoint = "dup")]
    private static extern int LibcDup(int fd);
}
