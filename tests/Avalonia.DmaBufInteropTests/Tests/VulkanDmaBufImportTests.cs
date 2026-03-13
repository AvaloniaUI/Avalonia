using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Avalonia.DmaBufInteropTests.NativeInterop;

namespace Avalonia.DmaBufInteropTests.Tests;

/// <summary>
/// Tests Vulkan DMA-BUF import path using raw Vulkan API via P/Invoke.
/// </summary>
internal static unsafe partial class VulkanDmaBufImportTests
{
    // Vulkan constants
    private const uint VK_API_VERSION_1_1 = (1u << 22) | (1u << 12);
    private const int VK_SUCCESS = 0;
    private const int VK_STRUCTURE_TYPE_APPLICATION_INFO = 0;
    private const int VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO = 1;
    private const int VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO = 3;
    private const int VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO = 2;
    private const int VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO = 5;
    private const int VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO = 14;
    private const int VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO = 15;
    private const int VK_STRUCTURE_TYPE_IMPORT_MEMORY_FD_INFO_KHR = 1000074000;
    private const int VK_STRUCTURE_TYPE_MEMORY_FD_PROPERTIES_KHR = 1000074001;
    private const int VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO = 1000127001;
    private const int VK_STRUCTURE_TYPE_IMAGE_DRM_FORMAT_MODIFIER_EXPLICIT_CREATE_INFO_EXT = 1000158004;
    private const int VK_STRUCTURE_TYPE_IMAGE_DRM_FORMAT_MODIFIER_PROPERTIES_EXT = 1000158005;
    private const int VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_DRM_FORMAT_MODIFIER_INFO_EXT = 1000158003;
    private const int VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO = 1000072001;
    private const int VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_IMAGE_FORMAT_INFO = 1000071000;
    private const int VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2 = 1000059003;
    private const int VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2 = 1000059003;
    private const int VK_STRUCTURE_TYPE_DRM_FORMAT_MODIFIER_PROPERTIES_LIST_EXT = 1000158000;
    private const int VK_STRUCTURE_TYPE_FORMAT_PROPERTIES_2 = 1000059005;

    private const int VK_IMAGE_TYPE_2D = 1;
    private const int VK_FORMAT_B8G8R8A8_UNORM = 44;
    private const int VK_IMAGE_TILING_DRM_FORMAT_MODIFIER_EXT = 1000158000;
    private const int VK_IMAGE_TILING_OPTIMAL = 0;
    private const int VK_IMAGE_USAGE_SAMPLED_BIT = 0x00000004;
    private const int VK_IMAGE_USAGE_TRANSFER_SRC_BIT = 0x00000001;
    private const int VK_SHARING_MODE_EXCLUSIVE = 0;
    private const int VK_SAMPLE_COUNT_1_BIT = 0x00000001;
    private const int VK_IMAGE_LAYOUT_UNDEFINED = 0;
    private const int VK_IMAGE_VIEW_TYPE_2D = 1;
    private const int VK_COMPONENT_SWIZZLE_IDENTITY = 0;
    private const int VK_IMAGE_ASPECT_COLOR_BIT = 0x00000001;
    private const uint VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT = 0x00000200;
    private const uint VK_QUEUE_FAMILY_FOREIGN_EXT = ~0u;
    private const uint VK_QUEUE_FAMILY_IGNORED = ~0u;

    private const string LibVulkan = "libvulkan.so.1";

    // Minimal Vulkan P/Invoke declarations
    [LibraryImport(LibVulkan, EntryPoint = "vkCreateInstance")]
    private static partial int VkCreateInstance(VkInstanceCreateInfo* createInfo, void* allocator, IntPtr* instance);

    [LibraryImport(LibVulkan, EntryPoint = "vkDestroyInstance")]
    private static partial void VkDestroyInstance(IntPtr instance, void* allocator);

    [LibraryImport(LibVulkan, EntryPoint = "vkEnumeratePhysicalDevices")]
    private static partial int VkEnumeratePhysicalDevices(IntPtr instance, uint* count, IntPtr* devices);

    [LibraryImport(LibVulkan, EntryPoint = "vkGetPhysicalDeviceQueueFamilyProperties")]
    private static partial void VkGetPhysicalDeviceQueueFamilyProperties(IntPtr physicalDevice, uint* count,
        VkQueueFamilyProperties* properties);

    [LibraryImport(LibVulkan, EntryPoint = "vkCreateDevice")]
    private static partial int VkCreateDevice(IntPtr physicalDevice, VkDeviceCreateInfo* createInfo, void* allocator,
        IntPtr* device);

    [LibraryImport(LibVulkan, EntryPoint = "vkDestroyDevice")]
    private static partial void VkDestroyDevice(IntPtr device, void* allocator);

    [LibraryImport(LibVulkan, EntryPoint = "vkGetDeviceProcAddr", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr VkGetDeviceProcAddr(IntPtr device, string name);

    [LibraryImport(LibVulkan, EntryPoint = "vkGetInstanceProcAddr", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr VkGetInstanceProcAddr(IntPtr instance, string name);

    [LibraryImport(LibVulkan, EntryPoint = "vkEnumerateDeviceExtensionProperties")]
    private static partial int VkEnumerateDeviceExtensionProperties(IntPtr physicalDevice, byte* layerName,
        uint* count, VkExtensionProperties* properties);

    [LibraryImport(LibVulkan, EntryPoint = "vkGetPhysicalDeviceMemoryProperties")]
    private static partial void VkGetPhysicalDeviceMemoryProperties(IntPtr physicalDevice,
        VkPhysicalDeviceMemoryProperties* memProperties);

    [LibraryImport(LibVulkan, EntryPoint = "vkGetPhysicalDeviceFormatProperties2")]
    private static partial void VkGetPhysicalDeviceFormatProperties2(IntPtr physicalDevice, int format,
        VkFormatProperties2Native* formatProperties);

    [LibraryImport(LibVulkan, EntryPoint = "vkCreateImage")]
    private static partial int VkCreateImage(IntPtr device, VkImageCreateInfo* createInfo, void* allocator,
        ulong* image);

    [LibraryImport(LibVulkan, EntryPoint = "vkDestroyImage")]
    private static partial void VkDestroyImage(IntPtr device, ulong image, void* allocator);

    [LibraryImport(LibVulkan, EntryPoint = "vkGetImageMemoryRequirements")]
    private static partial void VkGetImageMemoryRequirements(IntPtr device, ulong image,
        VkMemoryRequirements* memRequirements);

    [LibraryImport(LibVulkan, EntryPoint = "vkAllocateMemory")]
    private static partial int VkAllocateMemory(IntPtr device, VkMemoryAllocateInfo* allocateInfo, void* allocator,
        ulong* memory);

    [LibraryImport(LibVulkan, EntryPoint = "vkFreeMemory")]
    private static partial void VkFreeMemory(IntPtr device, ulong memory, void* allocator);

    [LibraryImport(LibVulkan, EntryPoint = "vkBindImageMemory")]
    private static partial int VkBindImageMemory(IntPtr device, ulong image, ulong memory, ulong offset);

    [LibraryImport(LibVulkan, EntryPoint = "vkCreateImageView")]
    private static partial int VkCreateImageView(IntPtr device, VkImageViewCreateInfo* createInfo, void* allocator,
        ulong* imageView);

    [LibraryImport(LibVulkan, EntryPoint = "vkDestroyImageView")]
    private static partial void VkDestroyImageView(IntPtr device, ulong imageView, void* allocator);

    // Function pointer delegate for vkGetMemoryFdPropertiesKHR
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int VkGetMemoryFdPropertiesKHRDelegate(IntPtr device, uint handleType, int fd,
        VkMemoryFdPropertiesKHRNative* memoryFdProperties);

    // Structures
    [StructLayout(LayoutKind.Sequential)]
    private struct VkApplicationInfo
    {
        public int sType;
        public void* pNext;
        public byte* pApplicationName;
        public uint applicationVersion;
        public byte* pEngineName;
        public uint engineVersion;
        public uint apiVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkInstanceCreateInfo
    {
        public int sType;
        public void* pNext;
        public uint flags;
        public VkApplicationInfo* pApplicationInfo;
        public uint enabledLayerCount;
        public byte** ppEnabledLayerNames;
        public uint enabledExtensionCount;
        public byte** ppEnabledExtensionNames;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkDeviceQueueCreateInfo
    {
        public int sType;
        public void* pNext;
        public uint flags;
        public uint queueFamilyIndex;
        public uint queueCount;
        public float* pQueuePriorities;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkDeviceCreateInfo
    {
        public int sType;
        public void* pNext;
        public uint flags;
        public uint queueCreateInfoCount;
        public VkDeviceQueueCreateInfo* pQueueCreateInfos;
        public uint enabledLayerCount;
        public byte** ppEnabledLayerNames;
        public uint enabledExtensionCount;
        public byte** ppEnabledExtensionNames;
        public void* pEnabledFeatures;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkExtensionProperties
    {
        public fixed byte extensionName[256];
        public uint specVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkQueueFamilyProperties
    {
        public uint queueFlags;
        public uint queueCount;
        public uint timestampValidBits;
        public uint minImageTransferGranularity_width;
        public uint minImageTransferGranularity_height;
        public uint minImageTransferGranularity_depth;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkPhysicalDeviceMemoryProperties
    {
        public uint memoryTypeCount;
        public VkMemoryType memoryTypes_0;
        public VkMemoryType memoryTypes_1;
        public VkMemoryType memoryTypes_2;
        public VkMemoryType memoryTypes_3;
        public VkMemoryType memoryTypes_4;
        public VkMemoryType memoryTypes_5;
        public VkMemoryType memoryTypes_6;
        public VkMemoryType memoryTypes_7;
        public VkMemoryType memoryTypes_8;
        public VkMemoryType memoryTypes_9;
        public VkMemoryType memoryTypes_10;
        public VkMemoryType memoryTypes_11;
        public VkMemoryType memoryTypes_12;
        public VkMemoryType memoryTypes_13;
        public VkMemoryType memoryTypes_14;
        public VkMemoryType memoryTypes_15;
        public VkMemoryType memoryTypes_16;
        public VkMemoryType memoryTypes_17;
        public VkMemoryType memoryTypes_18;
        public VkMemoryType memoryTypes_19;
        public VkMemoryType memoryTypes_20;
        public VkMemoryType memoryTypes_21;
        public VkMemoryType memoryTypes_22;
        public VkMemoryType memoryTypes_23;
        public VkMemoryType memoryTypes_24;
        public VkMemoryType memoryTypes_25;
        public VkMemoryType memoryTypes_26;
        public VkMemoryType memoryTypes_27;
        public VkMemoryType memoryTypes_28;
        public VkMemoryType memoryTypes_29;
        public VkMemoryType memoryTypes_30;
        public VkMemoryType memoryTypes_31;
        public uint memoryHeapCount;
        public VkMemoryHeap memoryHeaps_0;
        public VkMemoryHeap memoryHeaps_1;
        public VkMemoryHeap memoryHeaps_2;
        public VkMemoryHeap memoryHeaps_3;
        public VkMemoryHeap memoryHeaps_4;
        public VkMemoryHeap memoryHeaps_5;
        public VkMemoryHeap memoryHeaps_6;
        public VkMemoryHeap memoryHeaps_7;
        public VkMemoryHeap memoryHeaps_8;
        public VkMemoryHeap memoryHeaps_9;
        public VkMemoryHeap memoryHeaps_10;
        public VkMemoryHeap memoryHeaps_11;
        public VkMemoryHeap memoryHeaps_12;
        public VkMemoryHeap memoryHeaps_13;
        public VkMemoryHeap memoryHeaps_14;
        public VkMemoryHeap memoryHeaps_15;

        public VkMemoryType GetMemoryType(int index)
        {
            // Use pointer arithmetic since we can't use fixed buffer of structs
            fixed (VkMemoryType* p = &memoryTypes_0)
                return p[index];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkMemoryType
    {
        public uint propertyFlags;
        public uint heapIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkMemoryHeap
    {
        public ulong size;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkImageCreateInfo
    {
        public int sType;
        public void* pNext;
        public uint flags;
        public int imageType;
        public int format;
        public uint extent_width;
        public uint extent_height;
        public uint extent_depth;
        public uint mipLevels;
        public uint arrayLayers;
        public int samples;
        public int tiling;
        public int usage;
        public int sharingMode;
        public uint queueFamilyIndexCount;
        public uint* pQueueFamilyIndices;
        public int initialLayout;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkExternalMemoryImageCreateInfo
    {
        public int sType;
        public void* pNext;
        public uint handleTypes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkImageDrmFormatModifierExplicitCreateInfoEXT
    {
        public int sType;
        public void* pNext;
        public ulong drmFormatModifier;
        public uint drmFormatModifierPlaneCount;
        public VkSubresourceLayout* pPlaneLayouts;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkSubresourceLayout
    {
        public ulong offset;
        public ulong size;
        public ulong rowPitch;
        public ulong arrayPitch;
        public ulong depthPitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkMemoryRequirements
    {
        public ulong size;
        public ulong alignment;
        public uint memoryTypeBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkMemoryAllocateInfo
    {
        public int sType;
        public void* pNext;
        public ulong allocationSize;
        public uint memoryTypeIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkImportMemoryFdInfoKHR
    {
        public int sType;
        public void* pNext;
        public uint handleType;
        public int fd;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkMemoryDedicatedAllocateInfo
    {
        public int sType;
        public void* pNext;
        public ulong image;
        public ulong buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkMemoryFdPropertiesKHRNative
    {
        public int sType;
        public void* pNext;
        public uint memoryTypeBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkImageViewCreateInfo
    {
        public int sType;
        public void* pNext;
        public uint flags;
        public ulong image;
        public int viewType;
        public int format;
        public int components_r;
        public int components_g;
        public int components_b;
        public int components_a;
        public int subresourceRange_aspectMask;
        public uint subresourceRange_baseMipLevel;
        public uint subresourceRange_levelCount;
        public uint subresourceRange_baseArrayLayer;
        public uint subresourceRange_layerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkDrmFormatModifierPropertiesEXT
    {
        public ulong drmFormatModifier;
        public uint drmFormatModifierPlaneCount;
        public uint drmFormatModifierTilingFeatures;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkDrmFormatModifierPropertiesListEXT
    {
        public int sType;
        public void* pNext;
        public uint drmFormatModifierCount;
        public VkDrmFormatModifierPropertiesEXT* pDrmFormatModifierProperties;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkFormatProperties2Native
    {
        public int sType;
        public void* pNext;
        public uint linearTilingFeatures;
        public uint optimalTilingFeatures;
        public uint bufferFeatures;
    }

    public static List<TestResult> Run()
    {
        var results = new List<TestResult>();
        IntPtr instance = IntPtr.Zero;
        IntPtr device = IntPtr.Zero;
        IntPtr physicalDevice = IntPtr.Zero;

        try
        {
            var appInfo = new VkApplicationInfo
            {
                sType = VK_STRUCTURE_TYPE_APPLICATION_INFO,
                apiVersion = VK_API_VERSION_1_1
            };

            var instanceCreateInfo = new VkInstanceCreateInfo
            {
                sType = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
                pApplicationInfo = &appInfo
            };

            var result = VkCreateInstance(&instanceCreateInfo, null, &instance);
            if (result != VK_SUCCESS)
            {
                results.Add(new TestResult("Vulkan_DmaBuf_All", TestStatus.Skipped,
                    $"vkCreateInstance failed: {result}"));
                return results;
            }

            uint deviceCount = 0;
            VkEnumeratePhysicalDevices(instance, &deviceCount, null);
            if (deviceCount == 0)
            {
                results.Add(new TestResult("Vulkan_DmaBuf_All", TestStatus.Skipped, "no Vulkan devices"));
                return results;
            }

            var physDevices = stackalloc IntPtr[(int)deviceCount];
            VkEnumeratePhysicalDevices(instance, &deviceCount, physDevices);
            physicalDevice = physDevices[0];

            uint extCount = 0;
            VkEnumerateDeviceExtensionProperties(physicalDevice, null, &extCount, null);
            var extensions = new VkExtensionProperties[extCount];
            fixed (VkExtensionProperties* pExt = extensions)
                VkEnumerateDeviceExtensionProperties(physicalDevice, null, &extCount, pExt);

            var extNames = new HashSet<string>();
            fixed (VkExtensionProperties* pExts = extensions)
            {
                for (int i = 0; i < extCount; i++)
                    extNames.Add(Marshal.PtrToStringAnsi((IntPtr)pExts[i].extensionName) ?? "");
            }

            bool hasExternalMemory = extNames.Contains("VK_KHR_external_memory_fd");
            bool hasDmaBuf = extNames.Contains("VK_EXT_external_memory_dma_buf");
            bool hasDrmModifier = extNames.Contains("VK_EXT_image_drm_format_modifier");
            bool hasQueueFamilyForeign = extNames.Contains("VK_EXT_queue_family_foreign");

            results.Add(new TestResult("Vulkan_DmaBuf_Extension_Availability",
                hasDmaBuf && hasDrmModifier ? TestStatus.Passed : TestStatus.Skipped,
                $"external_memory_fd={hasExternalMemory}, dma_buf={hasDmaBuf}, drm_modifier={hasDrmModifier}, queue_family_foreign={hasQueueFamilyForeign}"));

            if (!hasDmaBuf || !hasDrmModifier || !hasExternalMemory)
            {
                results.Add(new TestResult("Vulkan_DmaBuf_All", TestStatus.Skipped,
                    "required extensions not available"));
                return results;
            }

            results.Add(TestModifierQuery(physicalDevice));

            var requiredExtensions = new List<string>
            {
                "VK_KHR_external_memory_fd",
                "VK_EXT_external_memory_dma_buf",
                "VK_EXT_image_drm_format_modifier"
            };
            if (hasQueueFamilyForeign)
                requiredExtensions.Add("VK_EXT_queue_family_foreign");

            uint queueFamilyCount = 0;
            VkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);
            var queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
            VkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamilies);

            uint graphicsQueueFamily = uint.MaxValue;
            for (uint i = 0; i < queueFamilyCount; i++)
            {
                if ((queueFamilies[i].queueFlags & 0x01) != 0)
                {
                    graphicsQueueFamily = i;
                    break;
                }
            }

            if (graphicsQueueFamily == uint.MaxValue)
            {
                results.Add(new TestResult("Vulkan_DmaBuf_All", TestStatus.Skipped, "no graphics queue"));
                return results;
            }

            var extNamePtrs = new IntPtr[requiredExtensions.Count];
            for (int i = 0; i < requiredExtensions.Count; i++)
                extNamePtrs[i] = Marshal.StringToHGlobalAnsi(requiredExtensions[i]);

            try
            {
                float priority = 1.0f;
                var queueCreateInfo = new VkDeviceQueueCreateInfo
                {
                    sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
                    queueFamilyIndex = graphicsQueueFamily,
                    queueCount = 1,
                    pQueuePriorities = &priority
                };

                fixed (IntPtr* pExtNames = extNamePtrs)
                {
                    var deviceCreateInfo = new VkDeviceCreateInfo
                    {
                        sType = VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
                        queueCreateInfoCount = 1,
                        pQueueCreateInfos = &queueCreateInfo,
                        enabledExtensionCount = (uint)requiredExtensions.Count,
                        ppEnabledExtensionNames = (byte**)pExtNames
                    };

                    result = VkCreateDevice(physicalDevice, &deviceCreateInfo, null, &device);
                }

                if (result != VK_SUCCESS)
                {
                    results.Add(new TestResult("Vulkan_DmaBuf_All", TestStatus.Failed,
                        $"vkCreateDevice failed: {result}"));
                    return results;
                }

                results.Add(TestDmaBufImageImport(physicalDevice, device, graphicsQueueFamily,
                    hasQueueFamilyForeign));
            }
            finally
            {
                for (int i = 0; i < extNamePtrs.Length; i++)
                    Marshal.FreeHGlobal(extNamePtrs[i]);
            }
        }
        finally
        {
            if (device != IntPtr.Zero)
                VkDestroyDevice(device, null);
            if (instance != IntPtr.Zero)
                VkDestroyInstance(instance, null);
        }

        return results;
    }

    private static TestResult TestModifierQuery(IntPtr physicalDevice)
    {
        var modList = new VkDrmFormatModifierPropertiesListEXT
        {
            sType = VK_STRUCTURE_TYPE_DRM_FORMAT_MODIFIER_PROPERTIES_LIST_EXT
        };
        var fmtProps = new VkFormatProperties2Native
        {
            sType = VK_STRUCTURE_TYPE_FORMAT_PROPERTIES_2,
            pNext = &modList
        };

        VkGetPhysicalDeviceFormatProperties2(physicalDevice, VK_FORMAT_B8G8R8A8_UNORM, &fmtProps);

        if (modList.drmFormatModifierCount == 0)
            return new TestResult("Vulkan_DmaBuf_Modifier_Query", TestStatus.Failed,
                "no DRM format modifiers for B8G8R8A8_UNORM");

        var modifiers = new VkDrmFormatModifierPropertiesEXT[modList.drmFormatModifierCount];
        fixed (VkDrmFormatModifierPropertiesEXT* pMods = modifiers)
        {
            modList.pDrmFormatModifierProperties = pMods;
            fmtProps.pNext = &modList;
            VkGetPhysicalDeviceFormatProperties2(physicalDevice, VK_FORMAT_B8G8R8A8_UNORM, &fmtProps);
        }

        bool hasLinear = false;
        foreach (var mod in modifiers)
            if (mod.drmFormatModifier == 0)
                hasLinear = true;

        return new TestResult("Vulkan_DmaBuf_Modifier_Query", TestStatus.Passed,
            $"{modList.drmFormatModifierCount} modifiers, linear={hasLinear}");
    }

    private static TestResult TestDmaBufImageImport(IntPtr physicalDevice, IntPtr device,
        uint graphicsQueueFamily, bool hasQueueFamilyForeign)
    {
        const uint width = 64, height = 64;

        using var allocator = new DmaBufAllocator();
        if (!allocator.IsAvailable)
            return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Skipped,
                "no DMA-BUF allocator available");

        using var alloc = allocator.AllocateLinear(width, height, GBM_FORMAT_ARGB8888, 0xFF00FF00);
        if (alloc == null)
            return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Skipped,
                "could not allocate DMA-BUF");

        var importFd = Dup(alloc.Fd);
        if (importFd < 0)
            return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed, "dup() failed");

        ulong vkImage = 0;
        ulong vkMemory = 0;
        ulong vkImageView = 0;

        try
        {
            var getMemFdPropsPtr = VkGetDeviceProcAddr(device, "vkGetMemoryFdPropertiesKHR");
            if (getMemFdPropsPtr == IntPtr.Zero)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    "vkGetMemoryFdPropertiesKHR not found");

            var getMemFdProps = Marshal.GetDelegateForFunctionPointer<VkGetMemoryFdPropertiesKHRDelegate>(getMemFdPropsPtr);

            var fdProps = new VkMemoryFdPropertiesKHRNative
            {
                sType = VK_STRUCTURE_TYPE_MEMORY_FD_PROPERTIES_KHR
            };
            var result = getMemFdProps(device, VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT, importFd, &fdProps);
            if (result != VK_SUCCESS)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    $"vkGetMemoryFdPropertiesKHR failed: {result}");

            var externalMemoryInfo = new VkExternalMemoryImageCreateInfo
            {
                sType = VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO,
                handleTypes = VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT
            };

            var planeLayout = new VkSubresourceLayout
            {
                offset = 0,
                rowPitch = alloc.Stride,
                size = 0,
                arrayPitch = 0,
                depthPitch = 0
            };

            var drmModifierInfo = new VkImageDrmFormatModifierExplicitCreateInfoEXT
            {
                sType = VK_STRUCTURE_TYPE_IMAGE_DRM_FORMAT_MODIFIER_EXPLICIT_CREATE_INFO_EXT,
                pNext = &externalMemoryInfo,
                drmFormatModifier = alloc.Modifier,
                drmFormatModifierPlaneCount = 1,
                pPlaneLayouts = &planeLayout
            };

            var imageCreateInfo = new VkImageCreateInfo
            {
                sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
                pNext = &drmModifierInfo,
                imageType = VK_IMAGE_TYPE_2D,
                format = VK_FORMAT_B8G8R8A8_UNORM,
                extent_width = width,
                extent_height = height,
                extent_depth = 1,
                mipLevels = 1,
                arrayLayers = 1,
                samples = VK_SAMPLE_COUNT_1_BIT,
                tiling = VK_IMAGE_TILING_DRM_FORMAT_MODIFIER_EXT,
                usage = VK_IMAGE_USAGE_SAMPLED_BIT,
                sharingMode = VK_SHARING_MODE_EXCLUSIVE,
                initialLayout = VK_IMAGE_LAYOUT_UNDEFINED
            };

            result = VkCreateImage(device, &imageCreateInfo, null, &vkImage);
            if (result != VK_SUCCESS)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    $"vkCreateImage failed: {result}");

            VkMemoryRequirements memReqs;
            VkGetImageMemoryRequirements(device, vkImage, &memReqs);

            VkPhysicalDeviceMemoryProperties memProps;
            VkGetPhysicalDeviceMemoryProperties(physicalDevice, &memProps);

            uint memTypeIndex = uint.MaxValue;
            uint compatBits = memReqs.memoryTypeBits & fdProps.memoryTypeBits;
            for (uint i = 0; i < memProps.memoryTypeCount; i++)
            {
                if ((compatBits & (1u << (int)i)) != 0)
                {
                    memTypeIndex = i;
                    break;
                }
            }

            if (memTypeIndex == uint.MaxValue)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    $"no compatible memory type (image={memReqs.memoryTypeBits:X}, fd={fdProps.memoryTypeBits:X})");

            var dedicatedInfo = new VkMemoryDedicatedAllocateInfo
            {
                sType = VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
                image = vkImage,
                buffer = 0
            };

            var importFdInfo = new VkImportMemoryFdInfoKHR
            {
                sType = VK_STRUCTURE_TYPE_IMPORT_MEMORY_FD_INFO_KHR,
                pNext = &dedicatedInfo,
                handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_DMA_BUF_BIT_EXT,
                fd = importFd
            };

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                pNext = &importFdInfo,
                allocationSize = memReqs.size,
                memoryTypeIndex = memTypeIndex
            };

            result = VkAllocateMemory(device, &allocInfo, null, &vkMemory);
            if (result != VK_SUCCESS)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    $"vkAllocateMemory failed: {result}");

            importFd = -1; // Vulkan took ownership

            result = VkBindImageMemory(device, vkImage, vkMemory, 0);
            if (result != VK_SUCCESS)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    $"vkBindImageMemory failed: {result}");

            var viewCreateInfo = new VkImageViewCreateInfo
            {
                sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                image = vkImage,
                viewType = VK_IMAGE_VIEW_TYPE_2D,
                format = VK_FORMAT_B8G8R8A8_UNORM,
                components_r = VK_COMPONENT_SWIZZLE_IDENTITY,
                components_g = VK_COMPONENT_SWIZZLE_IDENTITY,
                components_b = VK_COMPONENT_SWIZZLE_IDENTITY,
                components_a = VK_COMPONENT_SWIZZLE_IDENTITY,
                subresourceRange_aspectMask = VK_IMAGE_ASPECT_COLOR_BIT,
                subresourceRange_baseMipLevel = 0,
                subresourceRange_levelCount = 1,
                subresourceRange_baseArrayLayer = 0,
                subresourceRange_layerCount = 1
            };

            result = VkCreateImageView(device, &viewCreateInfo, null, &vkImageView);
            if (result != VK_SUCCESS)
                return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Failed,
                    $"vkCreateImageView failed: {result}");

            return new TestResult("Vulkan_DmaBuf_Image_Import", TestStatus.Passed,
                $"image + view created, memType={memTypeIndex}, modifier=0x{alloc.Modifier:X}");
        }
        finally
        {
            if (vkImageView != 0)
                VkDestroyImageView(device, vkImageView, null);
            if (vkMemory != 0)
                VkFreeMemory(device, vkMemory, null);
            if (vkImage != 0)
                VkDestroyImage(device, vkImage, null);
            if (importFd >= 0)
                Close(importFd);
        }
    }

    // We need dup() for duplicating the DMA-BUF fd
    [LibraryImport("libc", EntryPoint = "dup")]
    private static partial int Dup(int fd);
}
