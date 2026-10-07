namespace Avalonia.Platform;

public record struct PlatformGraphicsExternalImageProperties
{
    public int Width { get; set; }
    public int Height { get; set; }
    public PlatformGraphicsExternalImageFormat Format { get; set; }
    public ulong MemorySize { get; set; }
    public ulong MemoryOffset { get; set; }
    public bool TopLeftOrigin { get; set; }

    /// <summary>
    /// Vulkan-specific properties of the imported image, ignored by other backends.
    /// </summary>
    public PlatformGraphicsExternalImageVulkanProperties? VulkanProperties { get; set; }

    /// <summary>
    /// dma-buf-specific properties of the imported image.
    /// </summary>
    public PlatformGraphicsExternalImageDmaBufProperties? DmaBufProperties { get; set; }
}

public record struct PlatformGraphicsExternalImageVulkanProperties
{
    /// <summary>
    /// The VkImageLayout the underlying memory is currently in.
    /// </summary>
    public int Layout { get; set; }
}

/// <summary>
/// Describes the DRM layout of a dma-buf image.
/// </summary>
public record struct PlatformGraphicsExternalImageDmaBufProperties
{
    /// <summary>
    /// The DRM_FORMAT_MOD_INVALID modifier.
    /// </summary>
    public const ulong DrmModifierInvalid = 0x00ffffffffffffffUL;

    /// <summary>
    /// The DRM_FORMAT_* fourcc of the buffer.
    /// </summary>
    public uint DrmFormat { get; set; }

    /// <summary>
    /// The DRM_FORMAT_MOD_* layout modifier of the buffer, or <see cref="DrmModifierInvalid"/> for the implicit layout.
    /// </summary>
    public ulong DrmModifier { get; set; }

    /// <summary>
    /// The number of memory planes. When zero, a single plane described by the handle itself and
    /// <see cref="PlatformGraphicsExternalImageProperties.MemoryOffset"/> is assumed.
    /// </summary>
    public int PlaneCount { get; set; }

    /// <summary>
    /// Per-plane dma-buf file descriptors. When null, the handle's file descriptor is used for every plane.
    /// </summary>
    public int[]? PlaneFds { get; set; }

    /// <summary>
    /// Per-plane row pitches in bytes.
    /// </summary>
    public uint[]? PlaneStrides { get; set; }

    /// <summary>
    /// Per-plane byte offsets into the corresponding file descriptor.
    /// </summary>
    public uint[]? PlaneOffsets { get; set; }
}

public enum PlatformGraphicsExternalImageFormat
{
    R8G8B8A8UNorm,
    B8G8R8A8UNorm
}

/// <summary>
/// A DRM fourcc format + modifier pair the GPU backend can import as a
/// <see cref="KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor"/> image.
/// <see cref="Format"/> is a DRM_FORMAT_* fourcc; <see cref="Modifier"/> is a
/// DRM_FORMAT_MOD_* layout (<see cref="PlatformGraphicsExternalImageDmaBufProperties.DrmModifierInvalid"/>
/// meaning the implicit, driver-chosen layout).
/// </summary>
public readonly record struct PlatformGraphicsDrmFormat(uint Format, ulong Modifier);

/// <summary>
/// Describes various GPU memory handle types that are currently supported by Avalonia graphics backends
/// </summary>
public static class KnownPlatformGraphicsExternalImageHandleTypes
{
    /// <summary>
    /// An DXGI global shared handle returned by IDXGIResource::GetSharedHandle D3D11_RESOURCE_MISC_SHARED or D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX flag.
    /// The handle does not own the reference to the underlying video memory, so the provider should make sure that the resource is valid until
    /// the handle has been successfully imported
    /// </summary>
    public const string D3D11TextureGlobalSharedHandle = nameof(D3D11TextureGlobalSharedHandle);
    /// <summary>
    /// A DXGI NT handle returned by IDXGIResource1::CreateSharedHandle for a texture created with D3D11_RESOURCE_MISC_SHARED_NTHANDLE or flag
    /// </summary>
    public const string D3D11TextureNtHandle = nameof(D3D11TextureNtHandle);
    /// <summary>
    /// A POSIX file descriptor that's exported by Vulkan using VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT or in a compatible way
    /// </summary>
    public const string VulkanOpaquePosixFileDescriptor = nameof(VulkanOpaquePosixFileDescriptor);
    
    /// <summary>
    /// A NT handle that's been exported by Vulkan using VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_BIT or in a compatible way
    /// </summary>
    public const string VulkanOpaqueNtHandle = nameof(VulkanOpaqueNtHandle);
    
    // A global shared handle that's been exported by Vulkan using VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_KMT_BIT or in a compatible way
    public const string VulkanOpaqueKmtHandle = nameof(VulkanOpaqueKmtHandle);

    /// <summary>
    /// A reference to IOSurface
    /// </summary>
    public const string IOSurfaceRef = nameof(IOSurfaceRef);

    /// <summary>
    /// A Linux dma-buf file descriptor, imported via EGL_LINUX_DMA_BUF_EXT or VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT.
    /// </summary>
    public const string DmaBufFileDescriptor = nameof(DmaBufFileDescriptor);
}

/// <summary>
/// Describes various GPU semaphore handle types that are currently supported by Avalonia graphics backends
/// </summary>
public static class KnownPlatformGraphicsExternalSemaphoreHandleTypes
{
    /// <summary>
    /// A POSIX file descriptor that's been exported by Vulkan using VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT or in a compatible way
    /// </summary>
    public const string VulkanOpaquePosixFileDescriptor = nameof(VulkanOpaquePosixFileDescriptor);
    
    /// <summary>
    /// A NT handle that's been exported by Vulkan using VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_BIT or in a compatible way
    /// </summary>
    public const string VulkanOpaqueNtHandle = nameof(VulkanOpaqueNtHandle);
    
    // A global shared handle that's been exported by Vulkan using VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_KMT_BIT or in a compatible way
    public const string VulkanOpaqueKmtHandle = nameof(VulkanOpaqueKmtHandle);
    
    /// A DXGI NT handle returned by ID3D12Device::CreateSharedHandle or ID3D11Fence::CreateSharedHandle
    public const string Direct3D12FenceNtHandle = nameof(Direct3D12FenceNtHandle);
    
    /// <summary>
    /// A pointer to MTLSharedEvent object
    /// </summary>
    public const string MetalSharedEvent = nameof(MetalSharedEvent);
}
