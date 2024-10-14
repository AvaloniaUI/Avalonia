using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;

namespace Avalonia.Controls;

/// <summary>
/// Set of MacOS specific attached properties that allow deeper customization of the application per platform.
/// </summary>
public class MacOSProperties
{
    static MacOSProperties()
    {
        IsTemplateIconProperty.Changed.AddClassHandler<TrayIcon>(TrayIconIsTemplateIconChanged);
        CanBecomeKeyWindowProperty.Changed.AddClassHandler<Popup>(CanBecomeKeyWindowChanged);
    }

    /// <summary>
    /// Defines the IsTemplateIcon attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTemplateIconProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, TrayIcon, bool>("IsTemplateIcon");

    /// <summary>
    /// A Boolean value that determines whether the TrayIcon image represents a template image.
    /// </summary>
    public static void SetIsTemplateIcon(TrayIcon obj, bool value) => obj.SetValue(IsTemplateIconProperty, value);

    /// <summary>
    /// Returns a Boolean value that indicates whether the TrayIcon image is a template image.
    /// </summary>
    public static bool GetIsTemplateIcon(TrayIcon obj) => obj.GetValue(IsTemplateIconProperty);

    private static void TrayIconIsTemplateIconChanged(TrayIcon trayIcon, AvaloniaPropertyChangedEventArgs args)
    {
        (trayIcon.Impl as ITrayIconWithIsTemplateImpl)?.SetIsTemplateIcon(args.GetNewValue<bool>());
    }

    /// <summary>
    /// Defines the CanBecomeKeyWindow attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> CanBecomeKeyWindowProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, Popup, bool>("CanBecomeKeyWindow");

    /// <summary>
    /// A Boolean value that determines whether the native NSWindow can become key window.
    /// </summary>
    public static void SetCanBecomeKeyWindow(Popup popup, bool value) =>
        popup.SetValue(CanBecomeKeyWindowProperty, value);

    /// <summary>
    /// Returns a Boolean value that determines whether the native NSWindow can become key window.
    /// </summary>
    public static bool GetCanBecomeKeyWindow(Popup popup) => popup.GetValue(CanBecomeKeyWindowProperty);

    private static void CanBecomeKeyWindowChanged(Popup popup, AvaloniaPropertyChangedEventArgs args)
    {
        if (popup is not { Host: PopupRoot root })
        {
            return;
        }
        
        if (root.PlatformImpl is INativePopupImpl nativePopupImpl)
        {
            nativePopupImpl.CanBecomeKeyWindow = args.GetNewValue<bool>();
        }
    }
}
