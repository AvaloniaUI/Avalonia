using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Controls;

/// <summary>
/// Set of X11 specific properties and events that allow deeper customization of the application per platform.
/// </summary>
public class X11Properties
{
    public static readonly AttachedProperty<X11NetWmWindowType> NetWmWindowTypeProperty =
        AvaloniaProperty.RegisterAttached<X11Properties, Window, X11NetWmWindowType>("NetWmWindowType");

    public static void SetNetWmWindowType(Window obj, X11NetWmWindowType value) => obj.SetValue(NetWmWindowTypeProperty, value);
    public static X11NetWmWindowType GetNetWmWindowType(Window obj) => obj.GetValue(NetWmWindowTypeProperty);

    public static readonly AttachedProperty<string?> WmClassProperty =
        AvaloniaProperty.RegisterAttached<X11Properties, Window, string?>("WmClass");

    public static void SetWmClass(Window obj, string? value) => obj.SetValue(WmClassProperty, value);
    public static string? GetWmClass(Window obj) => obj.GetValue(WmClassProperty);

    static X11Properties()
    {
        NetWmWindowTypeProperty.Changed.Subscribe(OnNetWmWindowTypeChanged);
        WmClassProperty.Changed.Subscribe(OnWmClassChanged);
    }

    private static IX11OptionsToplevelImplFeature? TryGetFeature(AvaloniaPropertyChangedEventArgs e)
        => (e.Sender as TopLevel)?.PlatformImpl?.TryGetFeature<IX11OptionsToplevelImplFeature>();
    
    private static void OnWmClassChanged(AvaloniaPropertyChangedEventArgs<string?> e) => 
        TryGetFeature(e)?.SetWmClass(e.NewValue.GetValueOrDefault(null));

    private static void OnNetWmWindowTypeChanged(AvaloniaPropertyChangedEventArgs<X11NetWmWindowType> e) =>
        TryGetFeature(e)?.SetNetWmWindowType(e.NewValue.GetValueOrDefault());
}