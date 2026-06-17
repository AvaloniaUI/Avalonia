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

}