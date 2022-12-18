using System;
using Avalonia.SourceGenerator;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;
using uint32_t = System.UInt32;
using VkBool32 = System.UInt32;
namespace Avalonia.Vulkan;

internal unsafe partial class VulkanInstanceApi
{
    public IVulkanInstance Instance { get; }

    public VulkanInstanceApi(IVulkanInstance instance)
    {
        Instance = instance;
        Initialize(name => instance.GetInstanceProcAddress(instance.Handle, name));
    }

    [GetProcAddress("vkCreateDebugUtilsMessengerEXT", true)]
    public partial VkResult CreateDebugUtilsMessengerEXT(IntPtr instance,
        ref VkDebugUtilsMessengerCreateInfoEXT pCreateInfo, IntPtr pAllocator, out IntPtr pMessenger);
    
    [GetProcAddress("vkEnumeratePhysicalDevices")]
    public partial VkResult EnumeratePhysicalDevices(IntPtr instance, ref uint32_t pPhysicalDeviceCount,
        IntPtr* pPhysicalDevices);

    [GetProcAddress("vkGetPhysicalDeviceProperties")]
    public partial void GetPhysicalDeviceProperties(IntPtr physicalDevice, out VkPhysicalDeviceProperties pProperties);

    [GetProcAddress("vkEnumerateDeviceExtensionProperties")]
    public partial VkResult EnumerateDeviceExtensionProperties(IntPtr physicalDevice, byte* pLayerName,
        ref uint32_t pPropertyCount, VkExtensionProperties* pProperties);

    [GetProcAddress("vkGetPhysicalDeviceSurfaceSupportKHR")]
    public partial VkResult GetPhysicalDeviceSurfaceSupportKHR(IntPtr physicalDevice, uint32_t queueFamilyIndex,
        IntPtr surface, out VkBool32 pSupported);


    [GetProcAddress("vkGetPhysicalDeviceQueueFamilyProperties")]
    public partial void GetPhysicalDeviceQueueFamilyProperties(IntPtr physicalDevice,
        ref uint32_t pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties);

    [GetProcAddress("vkCreateDevice")]
    public partial VkResult CreateDevice(IntPtr physicalDevice, ref VkDeviceCreateInfo pCreateInfo,
        IntPtr pAllocator, out IntPtr pDevice);

    [GetProcAddress("vkGetDeviceQueue")]
    public partial void GetDeviceQueue(IntPtr device, uint32_t queueFamilyIndex, uint32_t queueIndex,
        out IntPtr pQueue);

    [GetProcAddress("vkGetDeviceProcAddr")]
    public partial IntPtr GetDeviceProcAddr(IntPtr device, IntPtr pName);
    
    [GetProcAddress("vkDestroySurfaceKHR")]
    public partial void DestroySurfaceKHR(IntPtr instance, IntPtr surface, IntPtr pAllocator);
    
    [GetProcAddress("vkGetPhysicalDeviceSurfaceFormatsKHR")]
    public partial VkResult GetPhysicalDeviceSurfaceFormatsKHR(
        IntPtr                            physicalDevice,
        IntPtr                                surface,
        ref uint32_t                                   pSurfaceFormatCount,
        VkSurfaceFormatKHR*                         pSurfaceFormats);

    [GetProcAddress("vkGetPhysicalDeviceMemoryProperties")]
    public partial void GetPhysicalDeviceMemoryProperties(IntPtr physicalDevice,
        out VkPhysicalDeviceMemoryProperties pMemoryProperties);

    [GetProcAddress("vkGetPhysicalDeviceSurfaceCapabilitiesKHR")]
    public partial VkResult GetPhysicalDeviceSurfaceCapabilitiesKHR(IntPtr physicalDevice, IntPtr surface,
        out VkSurfaceCapabilitiesKHR pSurfaceCapabilities);

    [GetProcAddress("vkGetPhysicalDeviceSurfacePresentModesKHR")]
    public partial VkResult GetPhysicalDeviceSurfacePresentModesKHR(IntPtr physicalDevice, IntPtr surface,
        ref uint32_t pPresentModeCount, VkPresentModeKHR* pPresentModes);
}