using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan
{
    public unsafe class VulkanPhysicalDevice
    {
        private VulkanPhysicalDevice(PhysicalDevice apiHandle, Vk api, uint queueCount, uint queueFamilyIndex)
        {
            InternalHandle = apiHandle;
            Api = api;
            QueueCount = queueCount;
            QueueFamilyIndex = queueFamilyIndex;
            
            api.GetPhysicalDeviceProperties(apiHandle, out var properties);
            
            DeviceName = Marshal.PtrToStringAnsi((IntPtr)properties.DeviceName);
            
            var version = (Version32)properties.ApiVersion;
            ApiVersion = new Version((int) version.Major, (int) version.Minor, 0, (int) version.Patch);
        }

        internal PhysicalDevice InternalHandle { get; }
        internal Vk Api { get; }
        public uint QueueCount { get; }
        public uint QueueFamilyIndex { get; }
        public IntPtr Handle => InternalHandle.Handle;

        public string DeviceName { get; }
        public Version ApiVersion { get; }

        internal static unsafe VulkanPhysicalDevice FindSuitablePhysicalDevice(VulkanInstance instance,
            VulkanSurface surface, bool preferDiscreteGpu, uint? preferredDevice)
        {
            uint physicalDeviceCount;

            instance.Api.EnumeratePhysicalDevices(instance.InternalHandle, &physicalDeviceCount, null).ThrowOnError();

            var physicalDevices = new PhysicalDevice[physicalDeviceCount];

            fixed (PhysicalDevice* pPhysicalDevices = physicalDevices)
            {
                instance.Api.EnumeratePhysicalDevices(instance.InternalHandle, &physicalDeviceCount, pPhysicalDevices)
                    .ThrowOnError();
            }

            var physicalDeviceProperties = new Dictionary<PhysicalDevice, PhysicalDeviceProperties>();

            foreach (var physicalDevice in physicalDevices)
            {
                instance.Api.GetPhysicalDeviceProperties(physicalDevice, out var properties);
                physicalDeviceProperties.Add(physicalDevice, properties);
            }

            if (preferredDevice.HasValue && preferredDevice != 0)
            {
                var physicalDevice = physicalDeviceProperties.FirstOrDefault(x => x.Value.DeviceID == preferredDevice);
                if (physicalDevice.Key.Handle != 0 && IsSuitableDevice(instance.Api, physicalDevice.Key,
                    physicalDevice.Value, surface.ApiHandle, out var queueCount,
                    out var queueFamilyIndex))
                    return new VulkanPhysicalDevice(physicalDevice.Key, instance.Api, queueCount, queueFamilyIndex);
            }

            if (preferDiscreteGpu)
            {
                var discreteGpus = physicalDeviceProperties.Where(p => p.Value.DeviceType == PhysicalDeviceType.DiscreteGpu);

                foreach (var gpu in discreteGpus)
                {
                    if (IsSuitableDevice(instance.Api, gpu.Key, gpu.Value, surface.ApiHandle, out var queueCount,
                    out var queueFamilyIndex))
                        return new VulkanPhysicalDevice(gpu.Key, instance.Api, queueCount, queueFamilyIndex);

                    physicalDeviceProperties.Remove(gpu.Key);
                }
            }

            foreach (var physicalDevice in physicalDeviceProperties)
                if (IsSuitableDevice(instance.Api, physicalDevice.Key, physicalDevice.Value, surface.ApiHandle, out var queueCount,
                    out var queueFamilyIndex))
                    return new VulkanPhysicalDevice(physicalDevice.Key, instance.Api, queueCount, queueFamilyIndex);

            throw new Exception("No suitable physical device found");
        }

        private static unsafe bool IsSuitableDevice(Vk api, PhysicalDevice physicalDevice, PhysicalDeviceProperties properties, SurfaceKHR surface,
            out uint queueCount, out uint familyIndex)
        {
            queueCount = 0;
            familyIndex = 0;

            if (properties.DeviceType == PhysicalDeviceType.Cpu) return false;

            var extensionMatches = 0;
            uint propertiesCount;

            api.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &propertiesCount, null).ThrowOnError();

            var extensionProperties = new ExtensionProperties[propertiesCount];

            fixed (ExtensionProperties* pExtensionProperties = extensionProperties)
            {
                api.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &propertiesCount,
                    pExtensionProperties).ThrowOnError();

                for (var i = 0; i < propertiesCount; i++)
                {
                    var extensionName = Marshal.PtrToStringAnsi((IntPtr)pExtensionProperties[i].ExtensionName);

                    if (VulkanDevice.RequiredDeviceExtensions.Contains(extensionName)) extensionMatches++;
                }
            }

            if (extensionMatches == VulkanDevice.RequiredDeviceExtensions.Count)
            {
                familyIndex = FindSuitableQueueFamily(api, physicalDevice, surface, out queueCount);

                return familyIndex != uint.MaxValue;
            }

            return false;
        }

        internal unsafe string[] GetSupportedExtensions()
        {
            uint propertiesCount;

            Api.EnumerateDeviceExtensionProperties(InternalHandle, (byte*)null, &propertiesCount, null).ThrowOnError();

            var extensionProperties = new ExtensionProperties[propertiesCount];

            fixed (ExtensionProperties* pExtensionProperties = extensionProperties)
            {
                Api.EnumerateDeviceExtensionProperties(InternalHandle, (byte*)null, &propertiesCount, pExtensionProperties)
                    .ThrowOnError();
            }

            return extensionProperties.Select(x => Marshal.PtrToStringAnsi((IntPtr)x.ExtensionName)).ToArray();
        }

        private static unsafe uint FindSuitableQueueFamily(Vk api, PhysicalDevice physicalDevice, SurfaceKHR surface,
            out uint queueCount)
        {
            const QueueFlags RequiredFlags = QueueFlags.QueueGraphicsBit | QueueFlags.QueueComputeBit;

            var khrSurface = new KhrSurface(api.Context);

            uint propertiesCount;

            api.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &propertiesCount, null);

            var properties = new QueueFamilyProperties[propertiesCount];

            fixed (QueueFamilyProperties* pProperties = properties)
            {
                api.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &propertiesCount, pProperties);
            }

            for (uint index = 0; index < propertiesCount; index++)
            {
                var queueFlags = properties[index].QueueFlags;

                khrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, index, surface, out var surfaceSupported)
                    .ThrowOnError();

                if (queueFlags.HasFlag(RequiredFlags) && surfaceSupported)
                {
                    queueCount = properties[index].QueueCount;
                    return index;
                }
            }

            queueCount = 0;
            return uint.MaxValue;
        }
    }
}
