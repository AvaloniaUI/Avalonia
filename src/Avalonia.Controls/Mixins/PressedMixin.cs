using Avalonia.Interactivity;
using Avalonia.Input;

namespace Avalonia.Controls.Mixins
{
    /// <summary>
    /// Adds pressed functionality to control classes.
    /// 
    /// Adds the ':pressed' class when the item is pressed.
    /// </summary>
    public static class PressedMixin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PressedMixin"/> class.
        /// </summary>
        /// <typeparam name="TControl">The control type.</typeparam>
        public static void Attach<TControl>() where TControl : Control
        {
            InputElement.PointerPressedEvent.AddClassHandler<TControl>((x, e) => HandlePointerPressed(x, e), RoutingStrategies.Tunnel);
            InputElement.PointerReleasedEvent.AddClassHandler<TControl>((x, e) => HandlePointerReleased(x), RoutingStrategies.Tunnel);
            InputElement.PointerCaptureLostEvent.AddClassHandler<TControl>((x, e) => HandlePointerReleased(x), RoutingStrategies.Tunnel);
        }

        private static void HandlePointerPressed<TControl>(TControl sender, PointerPressedEventArgs e) where TControl : Control
        {
            if (e.GetCurrentPoint(sender).Properties.IsLeftButtonPressed)
            {
                ((IPseudoClasses)sender.Classes).Set(":pressed", true);
            }
        }

        private static void HandlePointerReleased<TControl>(TControl sender) where TControl : Control
        {
            ((IPseudoClasses)sender.Classes).Set(":pressed", false);
        }
    }
}
