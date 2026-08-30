using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Vulkan;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.Vulkan;

internal class VulkanSupport
{
    [DllImport("vulkan-1.dll")]
    private static extern IntPtr vkGetInstanceProcAddr(IntPtr instance, string name);
    
    public static VulkanPlatformGraphics? TryInitialize(VulkanOptions options)
    {
        if (IsParallelsDisplayAdapter())
        {
            Logger.TryGet(LogEventLevel.Warning, "Vulkan")?.Log(null,
                "Parallels Display Adapter detected; skipping Vulkan initialization and falling back to the next configured rendering mode.");
            return null;
        }

        return VulkanPlatformGraphics.TryCreate(options ?? new(), new VulkanPlatformSpecificOptions
        {
            RequiredInstanceExtensions = { "VK_KHR_win32_surface" },
            GetProcAddressDelegate = vkGetInstanceProcAddr,
            DeviceCheckSurfaceFactory = instance => CreateHwndSurface(OffscreenParentWindow.Handle, instance),
            PlatformFeatures = new Dictionary<Type, object>
            {
                [typeof(IVulkanKhrSurfacePlatformSurfaceFactory)] = new VulkanSurfaceFactory()
            }
        });
    }

    private static unsafe bool IsParallelsDisplayAdapter()
    {
        var factoryGuid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory1));
        DirectXUnmanagedMethods.CreateDXGIFactory1(ref factoryGuid, out var factoryPointer);

        if (factoryPointer == null)
            return false;

        using var factory = MicroComRuntime.CreateProxyFor<IDXGIFactory1>(factoryPointer, true);

        for (uint index = 0; ; index++)
        {
            void* adapterPointer = null;

            if (factory.EnumAdapters1(index, &adapterPointer) != 0 || adapterPointer == null)
                return false;

            using var adapter = MicroComRuntime.CreateProxyFor<IDXGIAdapter1>(adapterPointer, true);
            var desc = adapter.Desc1;
            var description = Marshal.PtrToStringUni((IntPtr)desc.Description);

            if (description?.StartsWith(
                    "Parallels Display Adapter",
                    StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }
    }

    internal class VulkanSurfaceFactory : IVulkanKhrSurfacePlatformSurfaceFactory
    {
        public bool CanRenderToSurface(IVulkanPlatformGraphicsContext context, IPlatformRenderSurface surface) =>
            surface is INativePlatformHandleSurface handle && handle.HandleDescriptor == "HWND";

        public IVulkanKhrSurfacePlatformSurface CreateSurface(IVulkanPlatformGraphicsContext context, IPlatformRenderSurface handle) =>
            new HwndVulkanSurface((INativePlatformHandleSurface)handle);
    }

    class HwndVulkanSurface : IVulkanKhrSurfacePlatformSurface 
    {
        private readonly INativePlatformHandleSurface _handle;

        public HwndVulkanSurface(INativePlatformHandleSurface handle)
        {
            _handle = handle;
        }

        public void Dispose()
        {
            // No-op
        }

        public double Scaling => _handle.Scaling;
        public PixelSize Size => _handle.Size;
        public ulong CreateSurface(IVulkanPlatformGraphicsContext context) =>
            CreateHwndSurface(_handle.Handle, context.Instance);
    }

    private static ulong CreateHwndSurface(IntPtr window, IVulkanInstance instance)
    {
        var vulkanWin32 = new Win32VulkanInterface(instance);
        var createInfo = new VkWin32SurfaceCreateInfoKHR()
        {
            
            sType = VkWin32SurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
            hinstance =  UnmanagedMethods.GetModuleHandle(null),
            hwnd = window
        };
        VulkanException.ThrowOnError("vkCreateWin32SurfaceKHR",
            vulkanWin32.vkCreateWin32SurfaceKHR(instance.Handle, ref createInfo, IntPtr.Zero, out var surface));
        return surface;
    }
}

