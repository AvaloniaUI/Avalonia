using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Interop;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.Skia.RenderTests;

/// <summary>
/// Provides llvmpipe (GL) and lavapipe (Vulkan) platform graphics backed by the
/// unofficial.mesa.softwarerenderer native library.
/// </summary>
internal static unsafe class MesaSoftwareRenderer
{
    public static bool GlEnabled { get; } =
        Environment.GetEnvironmentVariable("AVALONIA_RENDER_TESTS_DISABLE_MESA_GL") != "1";

    public static bool VulkanEnabled { get; } =
        Environment.GetEnvironmentVariable("AVALONIA_RENDER_TESTS_DISABLE_MESA_VULKAN") != "1";

    private static readonly Lazy<IntPtr> s_library = new(() =>
        NativeLibrary.Load("softmesa", typeof(MesaSoftwareRenderer).Assembly, null));

    private static readonly Lazy<EglPlatformGraphics> s_gl = new(CreateGl);
    private static readonly Lazy<VulkanPlatformGraphics> s_vulkan = new(CreateVulkan);

    public static IPlatformGraphics Gl => s_gl.Value;
    public static IPlatformGraphics Vulkan => s_vulkan.Value;

    private static IntPtr GetProcAddress(IntPtr proc, string name)
    {
        var bytes = new byte[Encoding.UTF8.GetByteCount(name) + 1];
        Encoding.UTF8.GetBytes(name, bytes.AsSpan(0, bytes.Length - 1));
        fixed (byte* ptr = bytes)
            return ((delegate* unmanaged[Stdcall]<byte*, IntPtr>)proc)(ptr);
    }

    private static IntPtr GetInstanceProcAddress(IntPtr proc, IntPtr instance, string name)
    {
        var bytes = new byte[Encoding.UTF8.GetByteCount(name) + 1];
        Encoding.UTF8.GetBytes(name, bytes.AsSpan(0, bytes.Length - 1));
        fixed (byte* ptr = bytes)
            return ((delegate* unmanaged[Stdcall]<IntPtr, byte*, IntPtr>)proc)(instance, ptr);
    }

    private static EglPlatformGraphics CreateGl()
    {
        var eglGetProcAddress = NativeLibrary.GetExport(s_library.Value, "eglGetProcAddress");
        var egl = new EglInterface(name => GetProcAddress(eglGetProcAddress, name));

        var display = new EglDisplay(new EglDisplayCreationOptions
        {
            Egl = egl,
            SupportsMultipleContexts = true,
            SupportsContextSharing = true,
            AllowPbufferOnlyConfigs = true,
            // Windows build of Mesa uses the default platform for surfaceless operation
            PlatformType = OperatingSystem.IsWindows() ? null : EGL_PLATFORM_SURFACELESS_MESA
        });
        return new EglPlatformGraphics(display);
    }

    private static VulkanPlatformGraphics CreateVulkan()
    {
        var vkGetInstanceProcAddr = NativeLibrary.GetExport(s_library.Value, "vkGetInstanceProcAddr");
        VkGetInstanceProcAddressDelegate getProcAddress = (instance, name) =>
            GetInstanceProcAddress(vkGetInstanceProcAddr, instance, name);

        var platformOptions = new VulkanPlatformSpecificOptions
        {
            GetProcAddressDelegate = getProcAddress
        };
        // The softmesa build has no WSI support at all
        var instance = VulkanInstance.Create(new VulkanInstanceCreationOptions
        {
            ApplicationName = "Avalonia render tests",
            RequireSurfaceExtension = false
        }, platformOptions);
        var device = VulkanDevice.Create(instance,
            new VulkanDeviceCreationOptions { RequireSwapchainExtension = false }, platformOptions);
        return VulkanPlatformGraphics.TryCreate(new VulkanOptions { CustomSharedDevice = device }, platformOptions)
               ?? throw new InvalidOperationException("Unable to create lavapipe-based VulkanPlatformGraphics");
    }
}
