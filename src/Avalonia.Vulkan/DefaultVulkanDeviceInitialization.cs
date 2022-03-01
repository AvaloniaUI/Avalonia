using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    internal class DefaultVulkanDeviceInitialization : IVulkanDeviceInitialization
    {
        /// <inheritdoc/>
        public unsafe Device CreateDevice(Vk api, VulkanInstance instance, VulkanPhysicalDevice physicalDevice, VulkanOptions options)
        {
            var queuePriorities = stackalloc float[(int)physicalDevice.QueueCount];

            for (var i = 0; i < physicalDevice.QueueCount; i++)
                queuePriorities[i] = 1f;

            var features = new PhysicalDeviceFeatures();

            var queueCreateInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = physicalDevice.QueueFamilyIndex,
                QueueCount = physicalDevice.QueueCount,
                PQueuePriorities = queuePriorities
            };

            var enabledExtensions = VulkanDevice.RequiredDeviceExtensions.Union(
                options.DeviceExtensions.Intersect(physicalDevice.GetSupportedExtensions())).ToArray();

            var ppEnabledExtensions = stackalloc IntPtr[enabledExtensions.Length];

            for (var i = 0; i < enabledExtensions.Length; i++)
                ppEnabledExtensions[i] = Marshal.StringToHGlobalAnsi(enabledExtensions[i]);

            var deviceCreateInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = 1,
                PQueueCreateInfos = &queueCreateInfo,
                PpEnabledExtensionNames = (byte**)ppEnabledExtensions,
                EnabledExtensionCount = (uint)enabledExtensions.Length,
                PEnabledFeatures = &features
            };

            api.CreateDevice(physicalDevice.InternalHandle, in deviceCreateInfo, null, out var device)
                .ThrowOnError();

            for (var i = 0; i < enabledExtensions.Length; i++)
                Marshal.FreeHGlobal(ppEnabledExtensions[i]);

            return device;
        }
    }
}
