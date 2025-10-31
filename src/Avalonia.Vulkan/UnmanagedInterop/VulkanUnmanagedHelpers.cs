using System;

namespace Avalonia.Vulkan.UnmanagedInterop;

static class VulkanUnmanagedHelpers
{
    public static Version DecodeVersion(uint bits)
    {
        var major = (bits >> 22) & 0x7F;
        var minor = (bits >> 12) & 0x3FF;
        var patch = bits & 0xFFF;
        return new Version((int)major, (int)minor, (int)patch);
    }
}