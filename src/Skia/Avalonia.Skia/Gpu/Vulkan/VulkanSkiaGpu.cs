using System;
using System.Collections.Generic;
using Avalonia.Vulkan;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia.Vulkan;

internal class VulkanSkiaGpu : ISkiaGpu
{
    private readonly VulkanSkiaExternalObjectsFeature? _externalObjects;
    public IVulkanPlatformGraphicsContext Vulkan { get; private set; }
    public GRContext GrContext { get; private set; }

    public VulkanSkiaGpu(IVulkanPlatformGraphicsContext vulkan, long? maxResourceBytes)
    {
        Vulkan = vulkan;
        var device = vulkan.Device;
        using (Vulkan.EnsureCurrent())
        {
            IntPtr GetProcAddressWrapper(string name, IntPtr instance, IntPtr device)
            {
                if (device != IntPtr.Zero)
                {
                    var addr = Vulkan.Instance.GetDeviceProcAddress(device, name);
                    if (addr != IntPtr.Zero)
                        return addr;
                }

                if (instance != IntPtr.Zero)
                {
                    var addr = Vulkan.Instance.GetInstanceProcAddress(instance, name);
                    if (addr != IntPtr.Zero)
                        return addr;
                }

                return Vulkan.Instance.GetInstanceProcAddress(IntPtr.Zero, name);
            }

            var ctx = new GRVkBackendContext
            {
                VkInstance = device.Instance.Handle,
                VkPhysicalDevice = device.PhysicalDeviceHandle,
                VkDevice = device.Handle,
                VkQueue = device.MainQueueHandle,
                GraphicsQueueIndex = device.GraphicsQueueFamilyIndex,
                GetProcedureAddress = GetProcAddressWrapper
            };

            GrContext = GRContext.CreateVulkan(ctx) ??
                         throw new VulkanException("Unable to create GrContext from IVulkanDevice");
            
            if (maxResourceBytes.HasValue)
                GrContext.SetResourceCacheLimit(maxResourceBytes.Value);
        }

        if (vulkan.TryGetFeature<IVulkanContextExternalObjectsFeature>(out var externalObjects))
            _externalObjects = new VulkanSkiaExternalObjectsFeature(this, vulkan, externalObjects);
    }
    
    public void Dispose()
    {
        Vulkan.Dispose();
    }

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IExternalObjectsRenderInterfaceContextFeature))
            return _externalObjects;
        return null;
    }

    public bool IsLost => Vulkan.IsLost;
    public IDisposable EnsureCurrent() => Vulkan.EnsureCurrent();
    
    
    public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
    {
        var target = Vulkan.CreateRenderTarget(surfaces);
        return new VulkanSkiaRenderTarget(this, target);
    }

    
    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session) => null;
}