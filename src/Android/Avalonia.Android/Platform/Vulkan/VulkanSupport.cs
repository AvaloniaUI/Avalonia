using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Vulkan;

namespace Avalonia.Android.Platform.Vulkan
{
    internal class VulkanSupport
    {
        [DllImport("libvulkan.so")]
        private static extern IntPtr vkGetInstanceProcAddr(IntPtr instance, string name);

        public static VulkanPlatformGraphics? TryInitialize(VulkanOptions options) =>
            VulkanPlatformGraphics.TryCreate(options ?? new(), new VulkanPlatformSpecificOptions
                {
                    RequiredInstanceExtensions = { "VK_KHR_android_surface" },
                    GetProcAddressDelegate = vkGetInstanceProcAddr,
                    PlatformFeatures = new Dictionary<Type, object>
                    {
                        [typeof(IVulkanKhrSurfacePlatformSurfaceFactory)] = new VulkanSurfaceFactory()
                    }
                });

        internal class VulkanSurfaceFactory : IVulkanKhrSurfacePlatformSurfaceFactory
        {
            public bool CanRenderToSurface(IVulkanPlatformGraphicsContext context, object surface) =>
                surface is INativePlatformHandleSurface handle;

            public IVulkanKhrSurfacePlatformSurface CreateSurface(IVulkanPlatformGraphicsContext context, object handle) =>
                new AndroidVulkanSurface((INativePlatformHandleSurface)handle);
        }

        class AndroidVulkanSurface : IVulkanKhrSurfacePlatformSurface
        {
            private INativePlatformHandleSurface _handle;

            public AndroidVulkanSurface(INativePlatformHandleSurface handle)
            {
                _handle = handle;
            }

            public double Scaling => _handle.Scaling;
            public PixelSize Size => _handle.Size;
            public ulong CreateSurface(IVulkanPlatformGraphicsContext context) =>
                CreateAndroidSurface(_handle.Handle, context.Instance);

            public void Dispose()
            {
                // No-op
            }
        }

        private static ulong CreateAndroidSurface(nint handle, IVulkanInstance instance)
        {
            var vulkanAndroid = new AndroidVulkanInterface(instance);
            var createInfo = new VkAndroidSurfaceCreateInfoKHR()
            {

                sType = VkAndroidSurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR,
                window = handle
            };
            VulkanException.ThrowOnError("vkCreateAndroidSurfaceKHR",
                vulkanAndroid.vkCreateAndroidSurfaceKHR(instance.Handle, ref createInfo, IntPtr.Zero, out var surface));
            return surface;
        }
    }
}
