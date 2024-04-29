using System;
using Avalonia.SourceGenerator;
using Avalonia.Vulkan;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Win32.Vulkan;
partial class Win32VulkanInterface
{
    public Win32VulkanInterface(IVulkanInstance instance)
    {
        Initialize(name => instance.GetInstanceProcAddress(instance.Handle, name));
    }

    [GetProcAddress("vkCreateWin32SurfaceKHR")]
    public partial int vkCreateWin32SurfaceKHR(IntPtr instance, ref VkWin32SurfaceCreateInfoKHR pCreateInfo,
        IntPtr pAllocator, out ulong pSurface);
}

struct VkWin32SurfaceCreateInfoKHR
{
    public const uint VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR = 1000009000;
    public uint sType;
    public IntPtr pNext;
    public uint flags;
    public IntPtr hinstance;
    public IntPtr hwnd;
}
