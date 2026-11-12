using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.OpenGL;

/// <summary>
/// Renders a sample with Impeller unless RENDERER=skia is set. IMPELLER_BACKEND=vulkan prefers Vulkan over
/// OpenGL ES where the platform offers both.
/// </summary>
internal static class ImpellerSampleExtensions
{
    public static bool IsImpellerRequested =>
        !string.Equals(Environment.GetEnvironmentVariable("RENDERER"), "skia", StringComparison.OrdinalIgnoreCase);

    private static bool PreferVulkan =>
        string.Equals(Environment.GetEnvironmentVariable("IMPELLER_BACKEND"), "vulkan",
            StringComparison.OrdinalIgnoreCase);

    public static AppBuilder UseImpellerIfRequested(this AppBuilder builder, X11PlatformOptions? x11 = null,
        Win32PlatformOptions? win32 = null)
    {
        if (!IsImpellerRequested)
            return builder;

        x11 ??= new X11PlatformOptions();
        x11.RenderingMode = PreferVulkan
            ? [X11RenderingMode.Vulkan, X11RenderingMode.Egl]
            : [X11RenderingMode.Egl, X11RenderingMode.Vulkan];
        // Impeller's OpenGL backend needs OpenGL ES.
        x11.GlProfiles = new List<GlVersion>
        {
            new(GlProfileType.OpenGLES, 3, 2),
            new(GlProfileType.OpenGLES, 3, 0),
            new(GlProfileType.OpenGLES, 2, 0),
        };

        win32 ??= new Win32PlatformOptions();
        win32.RenderingMode = PreferVulkan
            ? [Win32RenderingMode.Vulkan, Win32RenderingMode.AngleEgl]
            : [Win32RenderingMode.AngleEgl, Win32RenderingMode.Vulkan];

        Console.WriteLine("Rendering with Impeller");
        return builder
            .With(x11)
            .With(win32)
            .With(new AvaloniaNativePlatformOptions { RenderingMode = [AvaloniaNativeRenderingMode.Metal] })
            .UseImpeller();
    }
}
