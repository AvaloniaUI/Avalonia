using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;
public interface IVulkanDevice : IDisposable, IOptionalFeatureProvider
{
    public IntPtr Handle { get; }
    public IntPtr PhysicalDeviceHandle { get; }
    public IntPtr MainQueueHandle { get; }
    public uint GraphicsQueueFamilyIndex { get; }
    public IVulkanInstance Instance { get; }
    bool IsLost { get; }
    public IDisposable Lock();
    public IEnumerable<string> EnabledExtensions { get; }
}

public interface IVulkanInstance
{
    public IntPtr Handle { get; }
    public IntPtr GetInstanceProcAddress(IntPtr instance, string name);
    public IntPtr GetDeviceProcAddress(IntPtr device, string name);
    public IEnumerable<string> EnabledExtensions { get; }
}

[NotClientImplementable]
public interface IVulkanPlatformGraphicsContext : IPlatformGraphicsContext
{
    IVulkanDevice Device { get; }
    IVulkanInstance Instance { get; }
    internal VulkanInstanceApi InstanceApi { get; }
    internal VulkanDeviceApi DeviceApi { get; }
    internal VkDevice DeviceHandle { get; }
    internal VkPhysicalDevice PhysicalDeviceHandle { get; }
    internal VkInstance InstanceHandle { get; }
    internal VkQueue MainQueueHandle { get; }
    internal uint GraphicsQueueFamilyIndex { get; }
    IVulkanRenderTarget CreateRenderTarget(IEnumerable<object> surfaces);
}