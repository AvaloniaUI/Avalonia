using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Avalonia.Vulkan.Skia
{
    public class VulkanSkiaGpu : ISkiaGpu
    {
        private readonly VulkanPlatformInterface _vulkanPlatformInterface;
        private readonly long? _maxResourceBytes;
        private GRVkBackendContext _grVkBackend;
        private bool _initialized;

        public GRContext GrContext { get; set; }

        public VulkanSkiaGpu(VulkanPlatformInterface vulkanPlatformInterface, long? maxResourceBytes)
        {
            _vulkanPlatformInterface = vulkanPlatformInterface;
            _maxResourceBytes = maxResourceBytes;
        }

        public static ISkiaGpu CreateGpu()
        {
            if (VulkanPlatformInterface.TryInitialize())
            {
                var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>() ?? new SkiaOptions();
                var platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
                var gpu = new VulkanSkiaGpu(platformInterface, skiaOptions.MaxGpuResourceSizeBytes);
                AvaloniaLocator.CurrentMutable.Bind<VulkanSkiaGpu>().ToConstant(gpu);

                return gpu;
            }

            return null;
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            GRVkGetProcedureAddressDelegate getProcedureDelegate = (name, instanceHandle, deviceHandle) =>
            {
                IntPtr address;

                if (deviceHandle != IntPtr.Zero)
                {
                    address = _vulkanPlatformInterface.Device.Api.GetDeviceProcAddr(new Device(deviceHandle), name);
                    if (address != IntPtr.Zero)
                        return address;

                    address = _vulkanPlatformInterface.Device.Api.GetDeviceProcAddr(new Device(_vulkanPlatformInterface.Device.Handle), name);

                    if (address != IntPtr.Zero)
                        return address;
                }

                address = _vulkanPlatformInterface.Device.Api.GetInstanceProcAddr(new Instance(_vulkanPlatformInterface.Instance.Handle), name);


                if (address == IntPtr.Zero)
                    address = _vulkanPlatformInterface.Device.Api.GetInstanceProcAddr(new Instance(instanceHandle), name);

                return address;
            };
            
            _grVkBackend = new GRVkBackendContext()
            {
                VkInstance = _vulkanPlatformInterface.Device.Handle,
                VkPhysicalDevice = _vulkanPlatformInterface.PhysicalDevice.Handle,
                VkDevice = _vulkanPlatformInterface.Device.Handle,
                VkQueue = _vulkanPlatformInterface.Device.Queue.Handle,
                GraphicsQueueIndex = _vulkanPlatformInterface.PhysicalDevice.QueueFamilyIndex,
                GetProcedureAddress = getProcedureDelegate
            };
            
            GrContext = GRContext.CreateVulkan(_grVkBackend);
            if (_maxResourceBytes.HasValue)
            {
                GrContext.SetResourceCacheLimit(_maxResourceBytes.Value);
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is IPlatformNativeSurfaceHandle handle)
                {
                    IVulkanPlatformSurface platformSurface = null;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        platformSurface = new Win32VulkanPlatformSurface(handle);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && handle.HandleDescriptor == "XID")
                    {
                        platformSurface = new X11VulkanPlatformSurface(handle);
                    }
#if NET6_0_OR_GREATER
                    else if (OperatingSystem.IsAndroid())
                    {
                        platformSurface = new AndroidVulkanPlatformSurface(handle);
                    }
#endif
                    else
                        continue;
                    
                    var vulkanRenderTarget = new VulkanRenderTarget(_vulkanPlatformInterface, platformSurface);
                    
                    Initialize();

                    vulkanRenderTarget.GrContext = GrContext;

                    return vulkanRenderTarget;
                }
            }

            return null;
        }

        public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            return null;
        }

    }
}
