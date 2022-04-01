using System;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanPlatformInterface : IDisposable
    {
        private static VulkanOptions s_options;

        private VulkanPlatformInterface(VulkanInstance instance)
        {
            Instance = instance;
            Api = instance.Api;
        }

        public VulkanPhysicalDevice PhysicalDevice { get; private set; }
        public VulkanInstance Instance { get; }
        public VulkanDevice Device { get; private set; }
        public Vk Api { get; }

        public void Dispose()
        {
            Device?.Dispose();
            Instance?.Dispose();
            Api?.Dispose();
        }

        private static VulkanPlatformInterface TryCreate()
        {
            try
            {
                s_options = AvaloniaLocator.Current.GetService<VulkanOptions>() ?? new VulkanOptions();

#if NET6_0_OR_GREATER
            if (OperatingSystem.IsAndroid())
                Silk.NET.Core.Loader.SearchPathContainer.Platform = Silk.NET.Core.Loader.UnderlyingPlatform.Android;
#endif

                var instance = VulkanInstance.Create(s_options);

                return new VulkanPlatformInterface(instance);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool TryInitialize()
        {
            var feature = TryCreate();
            if (feature != null)
            {
                AvaloniaLocator.CurrentMutable.Bind<VulkanPlatformInterface>().ToConstant(feature);
                return true;
            }

            return false;
        }

        public VulkanSurfaceRenderTarget CreateRenderTarget(IVulkanPlatformSurface platformSurface)
        {
            var surface = VulkanSurface.CreateSurface(Instance, platformSurface);

            try
            {
                if (Device == null)
                {
                    PhysicalDevice = VulkanPhysicalDevice.FindSuitablePhysicalDevice(Instance, surface, s_options.PreferDiscreteGpu, s_options.PreferredDevice);
                    Device = VulkanDevice.Create(Instance, PhysicalDevice, s_options);
                }
            }
            catch (Exception ex)
            {
                surface.Dispose();
            }

            return new VulkanSurfaceRenderTarget(this, surface);
        }
    }
}
