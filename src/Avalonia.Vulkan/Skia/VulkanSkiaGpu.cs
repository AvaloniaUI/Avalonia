using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly VulkanPlatformInterface _vulkan;
        private readonly long? _maxResourceBytes;
        private GRContext _grContext;
        private GRVkBackendContext _grVkBackend;
        private bool _initialized;

        public GRContext GrContext { get => _grContext; set => _grContext = value; }

        public VulkanSkiaGpu(VulkanPlatformInterface vulkan, long? maxResourceBytes)
        {
            _vulkan = vulkan;
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
            GRVkGetProcedureAddressDelegate getProc = (string name, IntPtr instanceHandle, IntPtr deviceHandle) =>
            {
                IntPtr addr = IntPtr.Zero;

                if (deviceHandle != IntPtr.Zero)
                {
                    addr = _vulkan.Device.Api.GetDeviceProcAddr(new Device(deviceHandle), name);
                    if (addr != IntPtr.Zero)
                        return addr;

                    addr = _vulkan.Device.Api.GetDeviceProcAddr(new Device(_vulkan.Device.Handle), name);

                    if (addr != IntPtr.Zero)
                        return addr;
                }

                addr = _vulkan.Device.Api.GetInstanceProcAddr(new Instance(_vulkan.Instance.Handle), name);


                if (addr == IntPtr.Zero)
                    addr = _vulkan.Device.Api.GetInstanceProcAddr(new Instance(instanceHandle), name);

                return addr;
            };
            
            _grVkBackend = new GRVkBackendContext()
            {
                VkInstance = _vulkan.Device.Handle,
                VkPhysicalDevice = _vulkan.PhysicalDevice.Handle,
                VkDevice = _vulkan.Device.Handle,
                VkQueue = _vulkan.Device.Queue.Handle,
                GraphicsQueueIndex = _vulkan.PhysicalDevice.QueueFamilyIndex,
                GetProcedureAddress = getProc
            };
            _grContext = GRContext.CreateVulkan(_grVkBackend);
            if (_maxResourceBytes.HasValue)
            {
                _grContext.SetResourceCacheLimit(_maxResourceBytes.Value);
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
                    
                    var vulkanRenderTarget = new VulkanRenderTarget(_vulkan, platformSurface);
                    
                    Initialize();

                    vulkanRenderTarget.GrContext = _grContext;

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
