using Avalonia.Platform;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

/// <summary>
/// Maps DRM fourcc format codes to Vulkan VkFormat values.
/// </summary>
internal static class VulkanDrmFormatMapping
{
    /// <summary>
    /// Maps a DRM fourcc code to its corresponding VkFormat.
    /// </summary>
    public static VkFormat? TryMapDrmFourccToVkFormat(uint fourcc)
    {
        return fourcc switch
        {
            PlatformGraphicsDrmFormats.DRM_FORMAT_ARGB8888 => VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
            PlatformGraphicsDrmFormats.DRM_FORMAT_XRGB8888 => VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
            PlatformGraphicsDrmFormats.DRM_FORMAT_ABGR8888 => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
            PlatformGraphicsDrmFormats.DRM_FORMAT_XBGR8888 => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
            _ => null
        };
    }
}
