using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Vulkan;
using JetBrains.Annotations;

namespace Avalonia.X11.Vulkan;

internal class VulkanSupport
{
    private static IntPtr s_offscreenWindow;

    [DllImport("libvulkan.so.1")]
    private static extern IntPtr vkGetInstanceProcAddr(IntPtr instance, string name);
    
    [CanBeNull]
    public static VulkanPlatformGraphics TryInitialize(X11Info info, VulkanOptions options)
    {
        s_offscreenWindow = XLib.XCreateSimpleWindow(info.DeferredDisplay,
            XLib.XDefaultRootWindow(info.DeferredDisplay), 0, 0, 1,
            1, 1, IntPtr.Zero, IntPtr.Zero);
        XLib.XSync(info.DeferredDisplay, true);

        return VulkanPlatformGraphics.TryCreate(options ?? new(), new VulkanPlatformSpecificOptions
        {
            RequiredInstanceExtensions = { "VK_KHR_xlib_surface" },
            GetProcAddressDelegate = vkGetInstanceProcAddr,
            DeviceCheckSurfaceFactory = instance => CreateDummySurface(info.DeferredDisplay, instance),
            PlatformFeatures = new Dictionary<Type, object>
            {
                [typeof(IVulkanKhrSurfacePlatformSurfaceFactory)] = new VulkanSurfaceFactory(info.DeferredDisplay)
            }
        });
    }

    internal class VulkanSurfaceFactory : IVulkanKhrSurfacePlatformSurfaceFactory
    {
        private readonly IntPtr _display;

        public VulkanSurfaceFactory(IntPtr display)
        {
            _display = display;
        }
        
        public bool CanRenderToSurface(IVulkanPlatformGraphicsContext context, object surface) =>
            surface is INativeHandlePlatformSurface handle && handle.HandleDescriptor == "XID";

        public IVulkanKhrSurfacePlatformSurface CreateSurface(IVulkanPlatformGraphicsContext context, object handle) => 
            new XidSurface(_display, (INativeHandlePlatformSurface)handle);
    }

    class XidSurface : IVulkanKhrSurfacePlatformSurface 
    {
        private readonly IntPtr _display;
        private readonly INativeHandlePlatformSurface _handle;

        public XidSurface(IntPtr display, INativeHandlePlatformSurface handle)
        {
            _display = display;
            _handle = handle;
        }

        public void Dispose()
        {
            // No-op
        }

        public double Scaling => _handle.Scaling;
        public PixelSize Size => _handle.Size;
        public ulong CreateSurface(IVulkanPlatformGraphicsContext context) =>
            CreateXlibSurface(_display, _handle.Handle, context.Instance);
    }

    private static ulong CreateDummySurface(IntPtr display, IVulkanInstance instance)
    {
        var surf = CreateXlibSurface(display, s_offscreenWindow, instance);
        XLib.XSync(display, true);
        return surf;
    }

    private static ulong CreateXlibSurface(IntPtr display, IntPtr window, IVulkanInstance instance)
    {
        var vulkanXlib = new X11VulkanInterface(instance);
        var createInfo = new VkXlibSurfaceCreateInfoKHR
        {
            sType = VkXlibSurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR,
            dpy = display,
            window = window
        };
        vulkanXlib.vkCreateXlibSurfaceKHR(instance.Handle, ref createInfo, IntPtr.Zero, out var surface)
            .ThrowOnError("vkCreateXlibSurfaceKHR");
        return surface;
    }
}

