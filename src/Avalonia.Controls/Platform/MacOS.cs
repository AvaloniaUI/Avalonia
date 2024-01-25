namespace Avalonia.Controls;

/// <summary>
/// Set of MacOS specific attached properties that allow deeper customization of the application per platform.
/// </summary>
public class MacOS
{
    static MacOS()
    {
        IsTemplateIconProperty.Changed.AddClassHandler<TrayIcon>(TrayIconIsTemplateIconChanged);
    }

    /// <summary>
    /// Defines the IsTemplateIcon attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTemplateIconProperty =
        AvaloniaProperty.RegisterAttached<MacOS, TrayIcon, bool>("IsTemplateIcon");

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
        trayIcon.SetIsTemplateIcon(args.GetNewValue<bool>());
    }
}
