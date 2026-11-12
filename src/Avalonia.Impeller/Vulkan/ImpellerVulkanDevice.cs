using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Vulkan;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>Impeller's own Vulkan instance, loaded through the platform's Vulkan loader.</summary>
internal sealed unsafe class ImpellerVulkanInstance : IVulkanInstance
{
    private readonly IVulkanInstance _loader;
    private readonly delegate* unmanaged<IntPtr, IntPtr, IntPtr> _getDeviceProcAddr;

    public ImpellerVulkanInstance(IntPtr handle, IVulkanInstance loader)
    {
        Handle = handle;
        _loader = loader;
        _getDeviceProcAddr = (delegate* unmanaged<IntPtr, IntPtr, IntPtr>)
            loader.GetInstanceProcAddress(handle, "vkGetDeviceProcAddr");
        if (_getDeviceProcAddr == null)
            throw new PlatformNotSupportedException("vkGetDeviceProcAddr is not available.");
    }

    public IntPtr Handle { get; }

    public IntPtr GetInstanceProcAddress(IntPtr instance, string name) =>
        _loader.GetInstanceProcAddress(instance, name);

    public IntPtr GetDeviceProcAddress(IntPtr device, string name)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(name);
        try
        {
            return _getDeviceProcAddr(device, utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    public IEnumerable<string> EnabledExtensions => [];
}

/// <summary>Impeller's own Vulkan device.</summary>
internal sealed unsafe class ImpellerVulkanDevice : IVulkanDevice
{
    private readonly object _lock = new();

    public ImpellerVulkanDevice(ImpellerContextVulkanInfo info, ImpellerVulkanInstance instance)
    {
        Instance = instance;
        Handle = info.Vk_logical_device;
        PhysicalDeviceHandle = info.Vk_physical_device;
        GraphicsQueueFamilyIndex = info.Graphics_queue_family_index;

        var getDeviceQueue = (delegate* unmanaged<IntPtr, uint, uint, IntPtr*, void>)
            instance.GetDeviceProcAddress(Handle, "vkGetDeviceQueue");
        IntPtr queue;
        getDeviceQueue(Handle, info.Graphics_queue_family_index, info.Graphics_queue_index, &queue);
        MainQueueHandle = queue;
    }

    public IntPtr Handle { get; }
    public IntPtr PhysicalDeviceHandle { get; }
    public IntPtr MainQueueHandle { get; }
    public uint GraphicsQueueFamilyIndex { get; }
    public IVulkanInstance Instance { get; }
    public bool IsLost => false;
    public IEnumerable<string> EnabledExtensions => [];

    public IDisposable Lock()
    {
        Monitor.Enter(_lock);
        return new Unlock(_lock);
    }

    private sealed class Unlock(object target) : IDisposable
    {
        public void Dispose() => Monitor.Exit(target);
    }

    public object? TryGetFeature(Type featureType) => null;

    public void Dispose()
    {
    }
}
