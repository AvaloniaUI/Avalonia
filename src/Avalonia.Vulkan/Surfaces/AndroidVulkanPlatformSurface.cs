using System;
using Avalonia.Platform;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan.Surfaces
{
    internal class AndroidVulkanPlatformSurface : IVulkanPlatformSurface
    {
        private readonly IPlatformNativeSurfaceHandle _surfaceHandle;

        public AndroidVulkanPlatformSurface(IPlatformNativeSurfaceHandle surfaceHandle)
        {
            _surfaceHandle = surfaceHandle;
        }

        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrAndroidSurface surfaceExtension))
            {
                var createInfo = new AndroidSurfaceCreateInfoKHR() {
                    Window = (nint*)_surfaceHandle.Handle, SType = StructureType.AndroidSurfaceCreateInfoKhr };

                surfaceExtension.CreateAndroidSurface(new Instance(instance.Handle), createInfo, null, out var surface).ThrowOnError();

                return surface;
            }

            throw new Exception("VK_KHR_android_surface is not available on this platform.");
        }

        public PixelSize SurfaceSize => _surfaceHandle.Size;

        public float Scaling => Math.Max(0, (float)_surfaceHandle.Scaling);

        public void Dispose()
        {
        }
    }
}
