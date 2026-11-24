using System;
using Avalonia.Platform;

namespace Avalonia.Wayland;

/// <summary>
/// Opt-in entry point for creating a Wayland <c>zwlr_layer_shell_v1</c>
/// surface. A layer surface is anchored to an output edge instead of being
/// placed in the normal window stack; it has no decorations and never appears
/// in the taskbar or the alt-tab switcher.
/// </summary>
/// <remarks>
/// Create the implementation on the UI thread and pass it to the existing
/// <c>Window(IWindowImpl)</c> constructor. The compositor must advertise
/// <c>zwlr_layer_shell_v1</c>; when it does not, this method throws instead
/// of silently falling back to an xdg toplevel.
/// </remarks>
public static class WaylandLayerShell
{
    /// <summary>
    /// True when the connected compositor advertised
    /// <c>zwlr_layer_shell_v1</c>. Only meaningful after the Wayland
    /// platform has initialised.
    /// </summary>
    public static bool IsSupported { get; internal set; }

    /// <summary>
    /// Creates an <see cref="IWindowImpl"/> backed by a layer surface
    /// configured by <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The layer, anchor, size, and keyboard mode.</param>
    /// <returns>A window implementation for <c>new Window(impl)</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The options are invalid: width and height must be finite and positive,
    /// and the namespace must not be empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The Wayland platform has not been initialised.
    /// </exception>
    /// <exception cref="AvaloniaWaylandException">
    /// The compositor does not advertise <c>zwlr_layer_shell_v1</c>.
    /// </exception>
    public static IWindowImpl CreateWindowImpl(LayerShellOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (AvaloniaLocator.Current.GetService<IWindowingPlatform>() is not WaylandTopLevelFactory factory)
            throw new InvalidOperationException(
                "The Wayland platform is not initialized. Call AppBuilder.UseWayland() before creating a layer surface.");

        return factory.CreateLayerShellWindow(options);
    }
}
