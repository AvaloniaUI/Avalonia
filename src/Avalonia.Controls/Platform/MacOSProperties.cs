using Avalonia.Controls.Platform;
using Avalonia.Layout;
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
        IsTrafficLightHostProperty.Changed.AddClassHandler<Control>((control, args) =>
            MacOSTrafficLightLayout.SetIsHost(control, args.GetNewValue<bool>()));
    }

    /// <summary>
    /// Defines the IsTemplateIcon attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTemplateIconProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, TrayIcon, bool>("IsTemplateIcon");

    /// <summary>
    /// Defines the <c>IsTrafficLightHost</c> attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTrafficLightHostProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, Control, bool>("IsTrafficLightHost");

    /// <summary>
    /// Defines the <c>TrafficLightSize</c> attached property.
    /// </summary>
    public static readonly AttachedProperty<Size> TrafficLightSizeProperty =
        AvaloniaProperty.RegisterAttached<MacOSProperties, Window, Size>("TrafficLightSize");

    /// <summary>
    /// A Boolean value that determines whether the TrayIcon image represents a template image.
    /// </summary>
    public static void SetIsTemplateIcon(TrayIcon obj, bool value) => obj.SetValue(IsTemplateIconProperty, value);

    /// <summary>
    /// Returns a Boolean value that indicates whether the TrayIcon image is a template image.
    /// </summary>
    public static bool GetIsTemplateIcon(TrayIcon obj) => obj.GetValue(IsTemplateIconProperty);

    /// <summary>
    /// Sets a value that indicates whether a control hosts the native window buttons.
    /// </summary>
    /// <param name="control">The control that reserves space for the buttons.</param>
    /// <param name="value"><see langword="true"/> to place the buttons in the control; otherwise, <see langword="false"/>.</param>
    /// <remarks>
    /// <para>
    /// The control's minimum size becomes the size of the button group, so a control without an explicit size
    /// fits the buttons exactly. In a larger control, the buttons are centered. Set <see cref="Layoutable.MinWidth"/>
    /// or <see cref="Layoutable.MinHeight"/> locally to replace the minimum size. A minimum size set in a control
    /// template doesn't reliably replace it, because both use <see cref="Avalonia.Data.BindingPriority.Template"/>.
    /// </para>
    /// <para>
    /// The buttons are native views, so they don't follow the control's clipping, opacity, or render transforms.
    /// They follow the control's position in the window, even when the content's flow direction differs from
    /// the native title bar's direction.
    /// </para>
    /// <para>
    /// The buttons can only be placed while <see cref="GetTrafficLightSize"/> is nonzero. If a window contains
    /// several visible hosts, the first one registered is used. Hiding or removing the host restores the system
    /// layout. Other platforms ignore this property.
    /// </para>
    /// </remarks>
    public static void SetIsTrafficLightHost(Control control, bool value) =>
        control.SetValue(IsTrafficLightHostProperty, value);

    /// <summary>
    /// Gets a value that indicates whether a control hosts the native window buttons.
    /// </summary>
    /// <param name="control">The control to check.</param>
    /// <returns><see langword="true"/> if the buttons are placed in the control; otherwise, <see langword="false"/>.</returns>
    public static bool GetIsTrafficLightHost(Control control) => control.GetValue(IsTrafficLightHostProperty);

    /// <summary>
    /// Gets the size of the native window button group in device-independent pixels.
    /// </summary>
    /// <param name="window">The window that contains the buttons.</param>
    /// <returns>The size of the button group, or a zero size while the buttons can't be placed.</returns>
    /// <remarks>
    /// The size is zero on other platforms, when the client area isn't extended, when the window isn't fully
    /// decorated, and while the window is in native full-screen mode. The platform sets this value.
    /// </remarks>
    public static Size GetTrafficLightSize(Window window) => window.GetValue(TrafficLightSizeProperty);

    private static void TrayIconIsTemplateIconChanged(TrayIcon trayIcon, AvaloniaPropertyChangedEventArgs args)
    {
        (trayIcon.Impl as ITrayIconWithIsTemplateImpl)?.SetIsTemplateIcon(args.GetNewValue<bool>());
    }
}
