namespace Avalonia.Platform;

/// <summary>
/// Provides DRM fourcc format constants and mapping to <see cref="PlatformGraphicsExternalImageFormat"/>.
/// </summary>
public static class PlatformGraphicsDrmFormats
{
    /// <summary>DRM_FORMAT_ARGB8888: little-endian BGRA in memory = B8G8R8A8UNorm. Most common WebKit output.</summary>
    public const uint DRM_FORMAT_ARGB8888 = 0x34325241;

    /// <summary>DRM_FORMAT_XRGB8888: little-endian BGRX in memory = B8G8R8A8UNorm (opaque, ignore alpha).</summary>
    public const uint DRM_FORMAT_XRGB8888 = 0x34325258;

    /// <summary>DRM_FORMAT_ABGR8888: little-endian RGBA in memory = R8G8B8A8UNorm.</summary>
    public const uint DRM_FORMAT_ABGR8888 = 0x34324241;

    /// <summary>DRM_FORMAT_XBGR8888: little-endian RGBX in memory = R8G8B8A8UNorm (opaque, ignore alpha).</summary>
    public const uint DRM_FORMAT_XBGR8888 = 0x34324258;

    /// <summary>DRM_FORMAT_MOD_LINEAR: linear (non-tiled) memory layout.</summary>
    public const ulong DRM_FORMAT_MOD_LINEAR = 0;

    /// <summary>DRM_FORMAT_MOD_INVALID: indicates an invalid or unknown modifier.</summary>
    public const ulong DRM_FORMAT_MOD_INVALID = 0x00FFFFFFFFFFFFFF;

    /// <summary>
    /// Attempts to map a DRM fourcc code to a <see cref="PlatformGraphicsExternalImageFormat"/>.
    /// </summary>
    /// <param name="fourcc">The DRM format fourcc code.</param>
    /// <returns>The corresponding format, or null if the fourcc code is not recognized.</returns>
    public static PlatformGraphicsExternalImageFormat? TryMapDrmFormat(uint fourcc)
    {
        return fourcc switch
        {
            DRM_FORMAT_ARGB8888 => PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm,
            DRM_FORMAT_XRGB8888 => PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm,
            DRM_FORMAT_ABGR8888 => PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
            DRM_FORMAT_XBGR8888 => PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
            _ => null
        };
    }
}
