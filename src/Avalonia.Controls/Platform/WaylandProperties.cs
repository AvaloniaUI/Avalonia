using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Controls;

/// <summary>
/// Set of Wayland specific properties and events that allow deeper customization of the application per platform.
/// </summary>
public class WaylandProperties
{
    public static readonly AttachedProperty<string?> AppIdProperty =
        AvaloniaProperty.RegisterAttached<WaylandProperties, Window, string?>("AppId");

    public static void SetAppId(Window obj, string? value) => obj.SetValue(AppIdProperty, value);
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
