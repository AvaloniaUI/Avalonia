using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public interface IVulkanDeviceInitialization
    {
        /// <summary>
        /// Creates a vulkan logical device.
        /// </summary>
        /// <param name="api">The Vulkan bindings api</param>
        /// <param name="instance">The vulkan instance</param>
        /// <param name="physicalDevice">The selected vulkan physical device</param>
        /// <param name="options">The VulkanOptions provided during initialization</param>
        /// <returns>The vulkan logical device</returns>
        Device CreateDevice(Vk api, VulkanInstance instance, VulkanPhysicalDevice physicalDevice, VulkanOptions options);
    }
}
