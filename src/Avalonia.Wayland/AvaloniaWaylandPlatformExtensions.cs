using System;
using Avalonia.Logging;
using Avalonia.Wayland;

// UseXxx are deliberately in global Avalonia namespace
// ReSharper disable once CheckNamespace
namespace Avalonia;

/// <summary>
/// <see cref="AppBuilder"/> extensions for enabling the Wayland windowing backend.
/// </summary>
public static class AvaloniaWaylandPlatformExtensions
{
    /// <summary>
    /// Configures the application to use the Wayland windowing backend. Options can be supplied by
    /// registering a <see cref="WaylandPlatformOptions"/> instance with <see cref="AvaloniaLocator"/>.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static AppBuilder UseWayland(this AppBuilder builder)
    {
        builder
            .UseStandardRuntimePlatformSubsystem()
            .UseWindowingSubsystem(() =>
                WaylandPlatform.Initialize(AvaloniaLocator.Current.GetService<WaylandPlatformOptions>() ??
                                           new WaylandPlatformOptions()));
        return builder;
    }

    /// <summary>
    /// Configures the application to use the Wayland windowing backend when a usable Wayland
    /// compositor is available, falling back to the previously configured windowing backend
    /// otherwise. Call it after <c>UseX11</c> or <c>UsePlatformDetect</c>, e. g.
    /// <c>.UsePlatformDetect().UseWaylandWithFallback()</c>. Does nothing on non-Linux platforms.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// No windowing backend was configured prior to this call (on Linux).
    /// </exception>
    public static AppBuilder UseWaylandWithFallback(this AppBuilder builder)
    {
        if (!OperatingSystem.IsLinux())
            return builder;

        var fallback = builder.WindowingSubsystemInitializer
            ?? throw new InvalidOperationException(
                "A fallback windowing backend must be configured before calling UseWaylandWithFallback, " +
                "e.g. via UseX11 or UsePlatformDetect.");

        return builder
            .UseStandardRuntimePlatformSubsystem()
            .UseWindowingSubsystem(() =>
            {
                var error = WaylandPlatform.TryInitialize(
                    AvaloniaLocator.Current.GetService<WaylandPlatformOptions>() ?? new WaylandPlatformOptions());
                if (error != null)
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Platform)?.Log(null,
                        "Unable to initialize the Wayland backend, falling back: {Error}", error.SourceException);
                    fallback();
                }
            });
    }
}