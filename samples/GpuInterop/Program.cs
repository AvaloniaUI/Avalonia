using System;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Vulkan;

namespace GpuInterop
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var demoType = OperatingSystem.IsWindows() && args.AsSpan().Contains("--d3d") ? DemoType.D3D11 : DemoType.Vulkan;
            BuildAvaloniaAppCore(demoType).StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaAppCore(DemoType.Vulkan);

        private static AppBuilder BuildAvaloniaAppCore(DemoType demoType) =>
            AppBuilder
                .Configure(() => new App { DemoType = demoType })
                .UsePlatformDetect()
                .With(new Win32PlatformOptions
                {
                    RenderingMode = [demoType == DemoType.D3D11 ? Win32RenderingMode.AngleEgl : Win32RenderingMode.Vulkan]
                })
                .With(new X11PlatformOptions
                {
                    RenderingMode = [X11RenderingMode.Vulkan]
                })
                .With(new VulkanOptions
                {
                    VulkanInstanceCreationOptions = new VulkanInstanceCreationOptions
                    {
                        UseDebug = true
                    }
                })
                .WithDeveloperTools()
                .LogToTrace(LogEventLevel.Debug, "Vulkan");
    }
}
