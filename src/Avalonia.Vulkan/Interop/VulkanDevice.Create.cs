using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

internal unsafe partial class VulkanDevice
{
    public static IVulkanDevice Create(IVulkanInstance instance,
        VulkanDeviceCreationOptions options, VulkanPlatformSpecificOptions platformOptions)
    {
        uint deviceCount = 0;
        var api = new VulkanInstanceApi(instance);
        var vkInstance = new VkInstance(instance.Handle);
        api.EnumeratePhysicalDevices(vkInstance, ref deviceCount, null)
            .ThrowOnError("vkEnumeratePhysicalDevices");

        if (deviceCount == 0)
            throw new VulkanException("No devices found");

        var devices = stackalloc VkPhysicalDevice[(int)deviceCount];
        api.EnumeratePhysicalDevices(vkInstance, ref deviceCount, devices)
            .ThrowOnError("vkEnumeratePhysicalDevices");

        var surfaceForProbePtr = platformOptions.DeviceCheckSurfaceFactory?.Invoke(api.Instance);
        var surfaceForProbe = surfaceForProbePtr.HasValue && surfaceForProbePtr.Value != 0
            ? new VkSurfaceKHR(surfaceForProbePtr.Value)
            : (VkSurfaceKHR?)null;

        DeviceInfo? compatibleDevice = null, discreteDevice = null;

        for (var c = 0; c < deviceCount; c++)
        {
            var info = CheckDevice(api, devices[c], options, surfaceForProbe);
            if (info != null)
            {
                compatibleDevice ??= info;
                if (info.Value.Type == VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU)
                    discreteDevice ??= info;
            }

            if (compatibleDevice != null && (discreteDevice != null || !options.PreferDiscreteGpu))
                break;
        }

        if (options.PreferDiscreteGpu && discreteDevice != null)
            compatibleDevice = discreteDevice;

        if (compatibleDevice == null)
            throw new VulkanException("No compatible devices found");

        var dev = compatibleDevice.Value;

        var queuePriorities = stackalloc float[(int)dev.QueueCount];
        for (var c = 0; c < dev.QueueCount; c++)
            queuePriorities[c] = 1f;

        var queueCreateInfo = new VkDeviceQueueCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
            queueFamilyIndex = dev.QueueFamilyIndex,
            queueCount = dev.QueueCount,
            pQueuePriorities = queuePriorities,
        };

        var enableExtensions =
            new HashSet<string>(options.DeviceExtensions.Concat(VulkanExternalObjectsFeature.RequiredDeviceExtensions));

        var enabledExtensions = enableExtensions
            .Intersect(dev.Extensions).Append(VK_KHR_swapchain).Distinct().ToArray();

        using var pEnabledExtensions = new Utf8BufferArray(enabledExtensions);

        var createInfo = new VkDeviceCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
            queueCreateInfoCount = 1,
            pQueueCreateInfos = &queueCreateInfo,
            ppEnabledExtensionNames = pEnabledExtensions,
            enabledExtensionCount = pEnabledExtensions.UCount,
        };

        api.CreateDevice(dev.PhysicalDevice, ref createInfo, IntPtr.Zero, out var createdDevice)
            .ThrowOnError("vkCreateDevice");

        api.GetDeviceQueue(createdDevice, dev.QueueFamilyIndex, 0, out var createdQueue);

        return new VulkanDevice(api.Instance, createdDevice, dev.PhysicalDevice, createdQueue,
            dev.QueueFamilyIndex, enabledExtensions);

    }

    struct DeviceInfo
    {
        public VkPhysicalDevice PhysicalDevice;
        public uint QueueFamilyIndex;
        public VkPhysicalDeviceType Type;
        public List<string> Extensions;
        public uint QueueCount;
    }

    static List<string> GetDeviceExtensions(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice)
    {
        uint propertyCount = 0;
        instance.EnumerateDeviceExtensionProperties(physicalDevice, null, ref propertyCount, null);
        var extensionProps = new VkExtensionProperties[propertyCount];
        var extensions = new List<string>((int)propertyCount);
        if (propertyCount != 0)
            fixed (VkExtensionProperties* ptr = extensionProps)
            {
                instance.EnumerateDeviceExtensionProperties(physicalDevice, null, ref propertyCount, ptr);

                for (var c = 0; c < propertyCount; c++)
                    extensions.Add(Marshal.PtrToStringAnsi(new IntPtr(ptr[c].extensionName))!);
            }

        return extensions;
    }

    private const string VK_KHR_swapchain = "VK_KHR_swapchain";

    static unsafe DeviceInfo? CheckDevice(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice,
        VulkanDeviceCreationOptions options, VkSurfaceKHR? surface)
    {
        instance.GetPhysicalDeviceProperties(physicalDevice, out var properties);

        var supportedExtensions = GetDeviceExtensions(instance, physicalDevice);
        if (!supportedExtensions.Contains(VK_KHR_swapchain))
            return null;
        
        uint familyCount = 0;
        instance.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref familyCount, null);
        var familyProperties = stackalloc VkQueueFamilyProperties[(int)familyCount];
        instance.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref familyCount, familyProperties);
        var requredFlags = VkQueueFlags.VK_QUEUE_GRAPHICS_BIT;
        if (options.RequireComputeBit)
            requredFlags |= VkQueueFlags.VK_QUEUE_COMPUTE_BIT;

        for (var c = 0; c < familyCount; c++)
        {
            if ((familyProperties[c].queueFlags & requredFlags) != requredFlags)
                continue;
            if (surface.HasValue)
            {
                instance.GetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)c, surface.Value, out var supported)
                    .ThrowOnError("vkGetPhysicalDeviceSurfaceSupportKHR");
                if (supported == 0)
                    continue;
            }

            return new DeviceInfo
            {
                PhysicalDevice = physicalDevice,
                Extensions = supportedExtensions,
                Type = properties.deviceType,
                QueueFamilyIndex = (uint)c,
                QueueCount = familyProperties[c].queueCount
            };

        }

        return null;
    }
}