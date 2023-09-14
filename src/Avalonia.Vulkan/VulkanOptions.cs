using System;
using System.Collections.Generic;

namespace Avalonia.Vulkan;

public class VulkanOptions
{
    public VulkanInstanceCreationOptions VulkanInstanceCreationOptions { get; set; } = new();
    public VulkanDeviceCreationOptions VulkanDeviceCreationOptions { get; set; } = new();
    public IVulkanDevice? CustomSharedDevice { get; set; }
}
public class VulkanInstanceCreationOptions
{
    public VkGetInstanceProcAddressDelegate? CustomGetProcAddressDelegate { get; set; }
    
    /// <summary>
    /// Sets the application name of the vulkan instance
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Specifies the vulkan api version to use
    /// </summary>
    public Version VulkanVersion{ get; set; } =  new Version(1, 1, 0);

    /// <summary>
    /// Specifies additional extensions to enable if available on the instance
    /// </summary>
    public IList<string> InstanceExtensions { get; set; } = new List<string>();

    /// <summary>
    /// Specifies layers to enable if available on the instance
    /// </summary>
    public IList<string> EnabledLayers { get; set; } = new List<string>();

    /// <summary>
    /// Enables the debug layer
    /// </summary>
    public bool UseDebug { get; set; }

    /*


    /// <summary>
    /// Sets the presentation mode the swapchain uses if available.
    /// </summary>
    //public PresentMode PresentMode { get; set; } = PresentMode.Mailbox;*/
}

public class VulkanDeviceCreationOptions
{
    /// <summary>
    /// Specifies extensions to enable if available on the logical device
    /// </summary>
    public IList<string> DeviceExtensions { get; set; } = new List<string>();
    
    /// <summary>
    /// Selects the first suitable discrete gpu available
    /// </summary>
    public bool PreferDiscreteGpu { get; set; }
    
    public bool RequireComputeBit { get; set; }
}

public class VulkanPlatformSpecificOptions
{
    public IList<string> RequiredInstanceExtensions { get; set; } = new List<string>();
    public VkGetInstanceProcAddressDelegate? GetProcAddressDelegate { get; set; }
    public Func<IVulkanInstance, ulong>? DeviceCheckSurfaceFactory { get; set; }
    public Dictionary<Type, object> PlatformFeatures { get; set; } = new();
}

public delegate IntPtr VkGetInstanceProcAddressDelegate(IntPtr instance, string name);