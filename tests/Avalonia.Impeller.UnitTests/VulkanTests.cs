using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan;
using Xunit;

namespace Avalonia.Impeller.UnitTests;

/// <summary>Impeller on Vulkan without a window.</summary>
public class VulkanTests
{
    private sealed unsafe class Loader : IVulkanInstance
    {
        private readonly delegate* unmanaged<IntPtr, IntPtr, IntPtr> _getInstanceProcAddr;

        private Loader(IntPtr getInstanceProcAddr) =>
            _getInstanceProcAddr = (delegate* unmanaged<IntPtr, IntPtr, IntPtr>)getInstanceProcAddr;

        public static Loader? TryCreate()
        {
            string[] names = OperatingSystem.IsMacOS()
                ? ["libvulkan.1.dylib", "/opt/homebrew/lib/libvulkan.1.dylib", "/usr/local/lib/libvulkan.1.dylib"]
                : OperatingSystem.IsWindows() ? ["vulkan-1.dll"] : ["libvulkan.so.1"];
            foreach (var name in names)
                if (NativeLibrary.TryLoad(name, out var library) &&
                    NativeLibrary.TryGetExport(library, "vkGetInstanceProcAddr", out var export))
                    return new Loader(export);
            return null;
        }

        public IntPtr Handle => IntPtr.Zero;

        public IntPtr GetInstanceProcAddress(IntPtr instance, string name)
        {
            var utf8 = Marshal.StringToCoTaskMemUTF8(name);
            try
            {
                return _getInstanceProcAddr(instance, utf8);
            }
            finally
            {
                Marshal.FreeCoTaskMem(utf8);
            }
        }

        public IntPtr GetDeviceProcAddress(IntPtr device, string name) => IntPtr.Zero;

        public IEnumerable<string> EnabledExtensions => [];
    }

    private static ImpellerVulkanBackendContext CreateBackend()
    {
        var loader = Loader.TryCreate();
        Assert.SkipWhen(loader is null, "No Vulkan loader.");
        try
        {
            return ImpellerVulkanBackendContext.Create(loader!, null, () => false, new ImpellerOptions());
        }
        catch (PlatformNotSupportedException e)
        {
            Assert.Skip($"No Vulkan device for Impeller: {e.Message}");
            throw;
        }
    }

    [Fact]
    public void Impeller_Creates_Its_Own_Vulkan_Device()
    {
        using var app = ImpellerTestApp.Start();
        using var backend = CreateBackend();

        var device = backend.VulkanContext.Device;
        Assert.NotEqual(IntPtr.Zero, device.Instance.Handle);
        Assert.NotEqual(IntPtr.Zero, device.Handle);
        Assert.NotEqual(IntPtr.Zero, device.MainQueueHandle);
        Assert.NotEqual(IntPtr.Zero, device.Instance.GetDeviceProcAddress(device.Handle, "vkQueueSubmit"));
    }

    [Fact]
    public void Texture_Layers_Are_Uploaded()
    {
        using var app = ImpellerTestApp.Start();
        using var backend = CreateBackend();

        using var layer = backend.CreateTextureLayer(new PixelSize(16, 16), new Vector(96, 96));
        using (var context = layer.CreateDrawingContext())
            context.DrawRectangle(Brushes.Red, null, new RoundedRect(new Rect(0, 0, 8, 8)));

        var texture = layer.GetTexture(backend);
        Assert.NotNull(texture);
        Assert.Same(texture, layer.GetTexture(backend));
    }

    // A CAMetalLayer without a window, presented through VK_EXT_metal_surface.
    private sealed unsafe class MetalLayerSurface : IVulkanKhrSurfacePlatformSurface
    {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        private readonly IntPtr _layer;

        [StructLayout(LayoutKind.Sequential)]
        private struct CGRect(double x, double y, double width, double height)
        {
            public double X = x, Y = y, Width = width, Height = height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VkMetalSurfaceCreateInfoEXT
        {
            public int SType;
            public IntPtr Next;
            public uint Flags;
            public IntPtr Layer;
        }

        [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC)] private static extern IntPtr objc_msgSend(IntPtr target, IntPtr selector);

        public MetalLayerSurface(PixelSize size)
        {
            NativeLibrary.Load("/System/Library/Frameworks/QuartzCore.framework/QuartzCore");
            _layer = objc_msgSend(objc_getClass("CAMetalLayer"), sel_registerName("new"));
            Resize(size);
        }

        public void Resize(PixelSize size)
        {
            Size = size;
            var msgSend = (delegate* unmanaged<IntPtr, IntPtr, CGRect, void>)NativeLibrary.GetExport(
                NativeLibrary.Load(ObjC), "objc_msgSend");
            msgSend(_layer, sel_registerName("setFrame:"), new CGRect(0, 0, size.Width, size.Height));
            var setSize = (delegate* unmanaged<IntPtr, IntPtr, double, double, void>)NativeLibrary.GetExport(
                NativeLibrary.Load(ObjC), "objc_msgSend");
            setSize(_layer, sel_registerName("setDrawableSize:"), size.Width, size.Height);
        }

        public double Scaling => 1;
        public PixelSize Size { get; private set; }

        public ulong CreateSurface(IVulkanPlatformGraphicsContext context)
        {
            var create = (delegate* unmanaged<IntPtr, VkMetalSurfaceCreateInfoEXT*, IntPtr, ulong*, int>)
                context.Instance.GetInstanceProcAddress(context.Instance.Handle, "vkCreateMetalSurfaceEXT");
            Assert.True(create != null, "vkCreateMetalSurfaceEXT is not available.");
            var info = new VkMetalSurfaceCreateInfoEXT { SType = 1000217000, Layer = _layer };
            ulong surface;
            Assert.Equal(0, create(context.Instance.Handle, &info, IntPtr.Zero, &surface));
            return surface;
        }

        public void Dispose() => objc_msgSend(_layer, sel_registerName("release"));
    }

    [Fact]
    public void Frames_Are_Presented_Through_A_Swapchain()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "Uses a CAMetalLayer surface.");
        using var app = ImpellerTestApp.Start();
        using var backend = CreateBackend();
        using var surface = new MetalLayerSurface(new PixelSize(64, 48));
        using var target = new ImpellerVulkanRenderTarget(backend, surface, ownsSurface: false);

        for (var frame = 0; frame < 4; frame++)
        {
            if (frame == 2)
                surface.Resize(new PixelSize(80, 60));
            var sceneInfo = new IRenderTarget.RenderTargetSceneInfo(surface.Size, 1, CompositionTransparencyLevel.None);
            using var context = target.CreateDrawingContext(sceneInfo, out _);
            Assert.Null(context.GetFeature(typeof(Avalonia.Skia.ISkiaSharpApiLeaseFeature)));
            context.Clear(Colors.White);
            context.DrawRectangle(Brushes.Red, new ImmutablePen(Brushes.Blue, 2),
                new RoundedRect(new Rect(4, 4, 30, 20), 4));
        }
    }
}
