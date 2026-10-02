using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Controls;

/// <summary>
/// Set of Wayland specific properties and events that allow deeper customization of the application per platform.
/// </summary>
public class WaylandProperties
{
    /// <summary>
    /// Defines the <c>AppId</c> attached property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specifies the application ID (<c>app_id</c>) sent to the Wayland compositor via <c>xdg_toplevel</c>
    /// for window management, application identification, and window grouping.
    /// </para>
    /// <para>
    /// In Wayland environments, client windows do not directly provide their own window icons. Instead,
    /// compositors, docks, and taskbars resolve the application icon, launcher identity, and notifications by
    /// matching the <c>AppId</c> against the filename of the application's installed <c>.desktop</c> file
    /// (for example, using <c>org.example.myapp</c> to match <c>org.example.myapp.desktop</c>).
    /// </para>
    /// </remarks>
    public static readonly AttachedProperty<string?> AppIdProperty =
        AvaloniaProperty.RegisterAttached<Window, string?>("AppId", typeof(WaylandProperties));

    /// <summary>
    /// Sets the value of the <c>AppId</c> attached property of the specified window.
    /// </summary>
    /// <param name="obj">The window.</param>
    /// <param name="value">The application ID value to set.</param>
    public static void SetAppId(Window obj, string? value) => obj.SetValue(AppIdProperty, value);

    /// <summary>
    /// Gets the value of the <c>AppId</c> attached property of the specified window.
    /// </summary>
    /// <param name="obj">The window.</param>
    /// <returns>The application ID for <paramref name="obj"/>.</returns>
    public static string? GetAppId(Window obj) => obj.GetValue(AppIdProperty);

    static WaylandProperties()
    {
        AppIdProperty.Changed.Subscribe(OnAppIdChanged);
    }

    private static IWaylandOptionsToplevelImplFeature? TryGetFeature(AvaloniaPropertyChangedEventArgs e)
        => (e.Sender as TopLevel)?.PlatformImpl?.TryGetFeature<IWaylandOptionsToplevelImplFeature>();

    private static void OnAppIdChanged(AvaloniaPropertyChangedEventArgs<string?> e) =>
        TryGetFeature(e)?.SetAppId(e.NewValue.GetValueOrDefault(null));
}
