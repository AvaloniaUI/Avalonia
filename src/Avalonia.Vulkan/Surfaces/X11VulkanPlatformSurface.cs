using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan.Surfaces
{
    internal class X11VulkanPlatformSurface : IVulkanPlatformSurface
    {
        private readonly IPlatformNativeSurfaceHandle _surfaceHandle;
        private IntPtr _display = IntPtr.Zero;

        [DllImport("libX11.so.6")]
        public static extern IntPtr XOpenDisplay(IntPtr display);
        
        [DllImport("libX11.so.6")]
        public static extern int XCloseDisplay(IntPtr display);

        internal X11VulkanPlatformSurface(IPlatformNativeSurfaceHandle surfaceHandleImpl)
        {
            _surfaceHandle = surfaceHandleImpl;
        }
        
        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrXlibSurface surfaceExtension))
            {
                _display = XOpenDisplay(IntPtr.Zero);
                var createInfo = new XlibSurfaceCreateInfoKHR()
                {
                    SType = StructureType.XlibSurfaceCreateInfoKhr,
                    Dpy = (nint*) _display.ToPointer(),
                    Window = _surfaceHandle.Handle
                };

                surfaceExtension.CreateXlibSurface(new Instance(instance.Handle), createInfo, null, out var surface).ThrowOnError();

                return surface;
            }

            throw new Exception("VK_KHR_xlib_surface is not available on this platform.");
        }

        public PixelSize SurfaceSize => _surfaceHandle.Size;

        public float Scaling => Math.Max(0, (float)_surfaceHandle.Scaling);

        public void Dispose()
        {
            if (_display != IntPtr.Zero)
            {
                XCloseDisplay(_display);
            }
        }
    }
}
