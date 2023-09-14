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
    public partial VkResult CreateDebugUtilsMessengerEXT(VkInstance instance,
        ref VkDebugUtilsMessengerCreateInfoEXT pCreateInfo, IntPtr pAllocator, out VkDebugUtilsMessengerEXT pMessenger);

    [GetProcAddress("vkEnumeratePhysicalDevices")]
    public partial VkResult EnumeratePhysicalDevices(VkInstance instance, ref uint32_t pPhysicalDeviceCount,
        VkPhysicalDevice* pPhysicalDevices);

    [GetProcAddress("vkGetPhysicalDeviceProperties")]
    public partial void GetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice,
        out VkPhysicalDeviceProperties pProperties);

    [GetProcAddress("vkEnumerateDeviceExtensionProperties")]
    public partial VkResult EnumerateDeviceExtensionProperties(VkPhysicalDevice physicalDevice, byte* pLayerName,
        ref uint32_t pPropertyCount, VkExtensionProperties* pProperties);

    [GetProcAddress("vkGetPhysicalDeviceSurfaceSupportKHR")]
    public partial VkResult GetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice physicalDevice,
        uint32_t queueFamilyIndex,
        VkSurfaceKHR surface, out VkBool32 pSupported);


    [GetProcAddress("vkGetPhysicalDeviceQueueFamilyProperties")]
    public partial void GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice,
        ref uint32_t pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties);

    [GetProcAddress("vkCreateDevice")]
    public partial VkResult CreateDevice(VkPhysicalDevice physicalDevice, ref VkDeviceCreateInfo pCreateInfo,
        IntPtr pAllocator, out VkDevice pDevice);

    [GetProcAddress("vkGetDeviceQueue")]
    public partial void GetDeviceQueue(VkDevice device, uint32_t queueFamilyIndex, uint32_t queueIndex,
        out VkQueue pQueue);

    [GetProcAddress("vkGetDeviceProcAddr")]
    public partial IntPtr GetDeviceProcAddr(VkDevice device, IntPtr pName);

    [GetProcAddress("vkDestroySurfaceKHR")]
    public partial void DestroySurfaceKHR(VkInstance instance, VkSurfaceKHR surface, IntPtr pAllocator);

    [GetProcAddress("vkGetPhysicalDeviceSurfaceFormatsKHR")]
    public partial VkResult GetPhysicalDeviceSurfaceFormatsKHR(
        VkPhysicalDevice physicalDevice,
        VkSurfaceKHR surface,
        ref uint32_t pSurfaceFormatCount,
        VkSurfaceFormatKHR* pSurfaceFormats);

    [GetProcAddress("vkGetPhysicalDeviceMemoryProperties")]
    public partial void GetPhysicalDeviceMemoryProperties(VkPhysicalDevice physicalDevice,
        out VkPhysicalDeviceMemoryProperties pMemoryProperties);

    [GetProcAddress("vkGetPhysicalDeviceSurfaceCapabilitiesKHR")]
    public partial VkResult GetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface,
        out VkSurfaceCapabilitiesKHR pSurfaceCapabilities);

    [GetProcAddress("vkGetPhysicalDeviceSurfacePresentModesKHR")]
    public partial VkResult GetPhysicalDeviceSurfacePresentModesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface,
        ref uint32_t pPresentModeCount, VkPresentModeKHR* pPresentModes);
    
    [GetProcAddress("vkGetPhysicalDeviceProperties2", true)]
    public partial void GetPhysicalDeviceProperties2(
        VkPhysicalDevice                            physicalDevice,
        VkPhysicalDeviceProperties2*                pProperties);
}