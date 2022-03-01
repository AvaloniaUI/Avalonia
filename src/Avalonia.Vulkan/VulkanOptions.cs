using System;
using System.Collections.Generic;

namespace Avalonia.Vulkan
{
    public class VulkanOptions
    {
        /// <summary>
        /// Sets the application name of the vulkan instance
        /// </summary>
        public string ApplicationName { get; set; }
        
        /// <summary>
        /// Specifies the vulkan api version to use
        /// </summary>
        public Version VulkanVersion{ get; set; } =  new Version(1, 1, 0);
        
        /// <summary>
        /// Specifies additional extensions to enable if available on the instance
        /// </summary>
        public IList<string> InstanceExtensions { get; set; } = new List<string>();
        
        /// <summary>
        /// Specifies extensions to enable if available on the logical device
        /// </summary>
        public IList<string> DeviceExtensions { get; set; } = new List<string>();
        
        /// <summary>
        /// Specifies layers to enable if available on the instance
        /// </summary>
        public IList<string> EnabledLayers { get; set; } = new List<string>();
        
        /// <summary>
        /// Enables the debug layer
        /// </summary>
        public bool UseDebug { get; set; }
        
        /// <summary>
        /// Selects the first suitable discrete gpu available
        /// </summary>
        public bool PreferDiscreteGpu { get; set; }
        
        /// <summary>
        /// Sets the initialization object to be use to initialize the logical device.
        /// </summary>
        public IVulkanDeviceInitialization VulkanDeviceInitialization { get; set; } = new DefaultVulkanDeviceInitialization();
        
        /// <summary>
        /// Sets the device to use if available and suitable.
        /// </summary>
        public uint? PreferredDevice { get; set; }
    }
}
