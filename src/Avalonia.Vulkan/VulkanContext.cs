using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal class VulkanContext : IVulkanPlatformGraphicsContext
{
    private readonly IVulkanKhrSurfacePlatformSurfaceFactory? _surfaceFactory;
    private readonly VulkanExternalObjectsFeature? _externalObjectsFeature;
    public IVulkanDevice Device { get; }
    public IVulkanInstance Instance => Device.Instance;
    
    public VulkanContext(IVulkanDevice device, Dictionary<Type, object> platformFeatures)
    {
        Device = device;
        using (device.Lock())
        {
            InstanceApi = new VulkanInstanceApi(device.Instance);
            DeviceApi = new VulkanDeviceApi(device);
            if (platformFeatures.TryGetValue(typeof(IVulkanKhrSurfacePlatformSurfaceFactory), out var factory))
                _surfaceFactory = (IVulkanKhrSurfacePlatformSurfaceFactory)factory;
        }

        if (
            VulkanExternalObjectsFeature.RequiredInstanceExtensions.All(ext => Instance.EnabledExtensions.Contains(ext))
            && VulkanExternalObjectsFeature.RequiredDeviceExtensions.All(ext => Device.EnabledExtensions.Contains(ext)))
        {
            _externalObjectsFeature = new VulkanExternalObjectsFeature(this);
        }
    }
    
    public void Dispose()
    {
        
    }

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IVulkanContextExternalObjectsFeature))
            return _externalObjectsFeature;
        return null;
    }

    public bool IsLost => Device.IsLost;
    public IDisposable EnsureCurrent() => Device.Lock();

    public VkDevice DeviceHandle => new (Device.Handle);
    public VkPhysicalDevice PhysicalDeviceHandle => new (Device.PhysicalDeviceHandle);
    public VkInstance InstanceHandle => new(Instance.Handle);
    public VkQueue MainQueueHandle => new(Device.MainQueueHandle);
    public uint GraphicsQueueFamilyIndex => Device.GraphicsQueueFamilyIndex;

    public VulkanInstanceApi InstanceApi { get; }
    public VulkanDeviceApi DeviceApi { get; }
    public IVulkanRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
    {
        foreach (var surf in surfaces)
        {
            IVulkanKhrSurfacePlatformSurface khrSurface;
            if (surf is IVulkanKhrSurfacePlatformSurface khr)
                khrSurface = khr;
            else if (_surfaceFactory?.CanRenderToSurface(this, surf) == true)
                khrSurface = _surfaceFactory.CreateSurface(this, surf);
            else 
                continue;
            return new VulkanKhrRenderTarget(khrSurface, this);
        }

        throw new VulkanException("Unable to find a suitable platform surface");
    }
}