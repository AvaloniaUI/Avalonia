using System;
using Avalonia.Platform;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan.Surfaces
{
    internal class Win32VulkanPlatformSurface : IVulkanPlatformSurface
    {
        private readonly IPlatformNativeSurfaceHandle _surfaceHandle;

        public Win32VulkanPlatformSurface(IPlatformNativeSurfaceHandle handle)
        {
            _surfaceHandle = handle;
        }
        
        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrWin32Surface surfaceExtension))
            {
                var createInfo = new Win32SurfaceCreateInfoKHR() { Hinstance = 0, Hwnd = _surfaceHandle.Handle, SType = StructureType.Win32SurfaceCreateInfoKhr };

                surfaceExtension.CreateWin32Surface(new Instance(instance.Handle), createInfo, null, out var surface).ThrowOnError();

                return surface;
            }

            throw new Exception("VK_KHR_win32_surface is not available on this platform.");
        }

        public PixelSize SurfaceSize => _surfaceHandle.Size;

        public float Scaling => Math.Max(0, (float)_surfaceHandle.Scaling);

        public void Dispose()
        {
        }
    }
}
