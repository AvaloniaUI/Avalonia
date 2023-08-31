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

    public IntPtr GetProcAddress(VkInstance instance, string name) => _vkGetProcAddress(instance.Handle, name);


    [GetProcAddress("vkEnumerateInstanceLayerProperties")]
    public partial VkResult EnumerateInstanceLayerProperties(ref uint pPropertyCount, VkLayerProperties* pProperties);

    [GetProcAddress("vkCreateInstance")]
    public partial VkResult vkCreateInstance(ref VkInstanceCreateInfo pCreateInfo, IntPtr pAllocator, out VkInstance pInstance);

    [GetProcAddress("vkEnumerateInstanceExtensionProperties")]
    public partial VkResult vkEnumerateInstanceExtensionProperties(IntPtr pLayerName, uint* pPropertyCount,
        VkExtensionProperties* pProperties);
}