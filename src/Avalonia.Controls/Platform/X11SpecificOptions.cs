using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Controls.Platform;

public enum X11NetWmWindowType
{
    Normal,
    Dialog,
    Utility,
    Menu,
    Toolbar,
    Splash,
    Dock,
    Desktop
}

public class X11SpecificOptions
{
    public static readonly AttachedProperty<X11NetWmWindowType> NetWmWindowTypeProperty =
        AvaloniaProperty.RegisterAttached<X11SpecificOptions, Window, X11NetWmWindowType>("NetWmWindowType");

    public static void SetNetWmWindowType(Window obj, X11NetWmWindowType value) => obj.SetValue(NetWmWindowTypeProperty, value);
    public static X11NetWmWindowType GetNetWmWindowType(Window obj) => obj.GetValue(NetWmWindowTypeProperty);

    public static readonly AttachedProperty<string?> WmClassProperty =
        AvaloniaProperty.RegisterAttached<X11SpecificOptions, Window, string?>("WmClass");

    public static void SetWmClass(Window obj, string? value) => obj.SetValue(WmClassProperty, value);
    public static string? GetWmClass(Window obj) => obj.GetValue(WmClassProperty);

    static X11SpecificOptions()
    {
        NetWmWindowTypeProperty.Changed.Subscribe(OnNetWmWindowTypeChanged);
        WmClassProperty.Changed.Subscribe(OnWmClassChanged);
    }

    private static IX11SpecificOptionsToplevelImplFeature? TryGetFeature(AvaloniaPropertyChangedEventArgs e)
        => (e.Sender as TopLevel)?.PlatformImpl?.TryGetFeature<IX11SpecificOptionsToplevelImplFeature>();
    
    private static void OnWmClassChanged(AvaloniaPropertyChangedEventArgs<string?> e) => 
        TryGetFeature(e)?.SetWmClass(e.NewValue.GetValueOrDefault(null));

    private static void OnNetWmWindowTypeChanged(AvaloniaPropertyChangedEventArgs<X11NetWmWindowType> e) =>
        TryGetFeature(e)?.SetNetWmWindowType(e.NewValue.GetValueOrDefault());
}

public interface IX11SpecificOptionsToplevelImplFeature
{
    void SetNetWmWindowType(X11NetWmWindowType type);
    void SetWmClass(string? className);
}