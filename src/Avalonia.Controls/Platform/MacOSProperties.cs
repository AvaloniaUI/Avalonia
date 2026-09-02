using Avalonia.Controls.Platform;
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
        TrafficLightPositionProperty.Changed.AddClassHandler<Window>(TrafficLightPositionChanged);
    }

    /// <summary>
    /// Defines the IsTemplateIcon attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTemplateIconProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, TrayIcon, bool>("IsTemplateIcon");

    /// <summary>
    /// Defines the <c>TrafficLightPosition</c> attached property.
    /// </summary>
    public static readonly AttachedProperty<Point?> TrafficLightPositionProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, Window, Point?>(
            "TrafficLightPosition",
            validate: IsValidTrafficLightPosition);

    /// <summary>
    /// A Boolean value that determines whether the TrayIcon image represents a template image.
    /// </summary>
    public static void SetIsTemplateIcon(TrayIcon obj, bool value) => obj.SetValue(IsTemplateIconProperty, value);

    /// <summary>
    /// Returns a Boolean value that indicates whether the TrayIcon image is a template image.
    /// </summary>
    public static bool GetIsTemplateIcon(TrayIcon obj) => obj.GetValue(IsTemplateIconProperty);

    /// <summary>
    /// Sets the top-leading position of the native macOS window buttons.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="value">The position in logical units, or <see langword="null"/> to use the system layout.</param>
    public static void SetTrafficLightPosition(Window window, Point? value) =>
        window.SetValue(TrafficLightPositionProperty, value);

    /// <summary>
    /// Gets the top-leading position of the native macOS window buttons.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <returns>The position in logical units, or <see langword="null"/> when using the system layout.</returns>
    public static Point? GetTrafficLightPosition(Window window) =>
        window.GetValue(TrafficLightPositionProperty);

    private static void TrayIconIsTemplateIconChanged(TrayIcon trayIcon, AvaloniaPropertyChangedEventArgs args)
    {
        (trayIcon.Impl as ITrayIconWithIsTemplateImpl)?.SetIsTemplateIcon(args.GetNewValue<bool>());
    }

    private static void TrafficLightPositionChanged(Window window, AvaloniaPropertyChangedEventArgs args)
    {
        (window.PlatformImpl as IMacOSOptionsTopLevelImpl)?.SetTrafficLightPosition(args.GetNewValue<Point?>());
    }

    private static bool IsValidTrafficLightPosition(Point? position) =>
        position is null ||
        (position.Value.X >= 0 &&
            position.Value.Y >= 0 &&
            double.IsFinite(position.Value.X) &&
            double.IsFinite(position.Value.Y));
}
