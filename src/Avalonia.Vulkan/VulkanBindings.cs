using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Vulkan.Interop;

namespace Avalonia.Vulkan;


static class VulkanHelpers
{
    public static uint MakeVersion(Version v) => MakeVersion(v.Major, v.Minor, v.Build);

    public static uint MakeVersion(int major, int minor, int patch)
    {
        return (uint)((major << 22) | (minor << 12) | patch);
    }
    
    public const uint QueueFamilyIgnored = 4294967295;
}