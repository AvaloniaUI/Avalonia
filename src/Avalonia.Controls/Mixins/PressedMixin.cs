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

        public static void Attach<TControl>() where TControl : Control
        {
            InputElement.PointerPressedEvent.AddClassHandler<TControl>((x, e) => HandlePointerPressed(x, e), RoutingStrategies.Tunnel, true);
            InputElement.PointerReleasedEvent.AddClassHandler<TControl>((x, e) => HandlePointerReleased(x), RoutingStrategies.Tunnel, true);
            InputElement.PointerCaptureLostEvent.AddClassHandler<TControl>((x, e) => HandlePointerReleased(x), RoutingStrategies.Tunnel, true);
        }
    }
}
