using System;
using Avalonia.SourceGenerator;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.X11.Vulkan;
partial class X11VulkanInterface
{
    
    public X11VulkanInterface(IVulkanInstance instance)
    {
        Initialize(name => instance.GetInstanceProcAddress(instance.Handle, name));
    }

    [GetProcAddress("vkCreateXlibSurfaceKHR")]
    public partial int vkCreateXlibSurfaceKHR(IntPtr instance, ref VkXlibSurfaceCreateInfoKHR pCreateInfo,
        IntPtr pAllocator, out ulong pSurface);
}

struct VkXlibSurfaceCreateInfoKHR
{
    public const uint VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR = 1000004000;
    public uint sType;
    public IntPtr pNext;
    public uint flags;
    public IntPtr dpy;
    public IntPtr window;
}