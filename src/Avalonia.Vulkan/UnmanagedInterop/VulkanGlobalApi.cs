using System;
using Avalonia.SourceGenerator;
using Avalonia.Vulkan.Interop;

namespace Avalonia.Vulkan.UnmanagedInterop;

internal unsafe partial class VulkanGlobalApi
{
    private readonly VkGetInstanceProcAddressDelegate _vkGetProcAddress;

    public VulkanGlobalApi(VkGetInstanceProcAddressDelegate vkGetProcAddress)
    {
        _vkGetProcAddress = vkGetProcAddress;
        Initialize(name => vkGetProcAddress(IntPtr.Zero, name));
    }

    public IntPtr GetProcAddress(IntPtr instance, string name) => _vkGetProcAddress(instance, name);


    [GetProcAddress("vkEnumerateInstanceLayerProperties")]
    public partial VkResult EnumerateInstanceLayerProperties(ref uint pPropertyCount, VkLayerProperties* pProperties);

    [GetProcAddress("vkCreateInstance")]
    public partial VkResult vkCreateInstance(ref VkInstanceCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pInstance);
}