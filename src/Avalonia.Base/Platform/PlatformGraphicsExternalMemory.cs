namespace Avalonia.Platform;

public record struct PlatformGraphicsExternalImageProperties
{
    public int Width { get; set; }
    public int Height { get; set; }
    public PlatformGraphicsExternalImageFormat Format { get; set; }
    public ulong MemorySize { get; set; }
    public ulong MemoryOffset { get; set; }
    public bool TopLeftOrigin { get; set; }

    // DMA-BUF specific (ignored for other handle types)

    /// <summary>
    /// DRM format fourcc code (e.g., DRM_FORMAT_ARGB8888). Used by Vulkan and EGL DMA-BUF import paths.
    /// </summary>
    public uint DrmFourcc { get; set; }

    /// <summary>
    /// DRM format modifier (e.g., DRM_FORMAT_MOD_LINEAR). Determines memory layout (linear, tiled, compressed).
    /// </summary>
    public ulong DrmModifier { get; set; }

    /// <summary>
    /// Row stride in bytes for plane 0.
    /// </summary>
    public uint RowPitch { get; set; }

    /// <summary>
    /// Additional plane information for multi-plane DMA-BUF formats (planes 1-3). Null for single-plane formats.
    /// </summary>
    public DmaBufPlaneInfo[]? AdditionalPlanes { get; set; }
}

/// <summary>
/// Describes a single plane of a multi-plane DMA-BUF buffer.
/// </summary>
public record struct DmaBufPlaneInfo
{
    /// <summary>
    /// DMA-BUF file descriptor for this plane.
    /// </summary>
    public int Fd { get; set; }

    /// <summary>
    /// Byte offset into the plane.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Row stride for this plane.
    /// </summary>
    public uint Pitch { get; set; }

    /// <summary>
    /// DRM format modifier (usually same as plane 0).
    /// </summary>
    public ulong Modifier { get; set; }
}

public enum PlatformGraphicsExternalImageFormat
{
    R8G8B8A8UNorm,
    B8G8R8A8UNorm
}

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
    /// A Linux DMA-BUF file descriptor. Imported via VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT (Vulkan)
    /// or EGL_LINUX_DMA_BUF_EXT (EGL). Semantically distinct from VulkanOpaquePosixFileDescriptor as DMA-BUF
    /// fds carry DRM format modifier metadata and use a different import path.
    /// </summary>
    public const string DmaBufFileDescriptor = "DMABUF_FD";
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

    /// <summary>
    /// A Linux sync fence file descriptor. Maps to VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_SYNC_FD_BIT (Vulkan)
    /// or EGL_ANDROID_native_fence_sync (EGL). Uses temporary import semantics — the semaphore payload
    /// is consumed on the first wait.
    /// </summary>
    public const string SyncFileDescriptor = "SYNC_FD";
}
