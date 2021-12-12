namespace Avalonia.Input
{
    public class InputMethod
    {
        /// <summary>
        ///     A dependency property that enables alternative text inputs.
        /// </summary>
        public static readonly AvaloniaProperty<bool> IsInputMethodEnabledProperty =
            AvaloniaProperty.RegisterAttached<InputMethod, InputElement, bool>("IsInputMethodEnabled", true);
        
        /// <summary>
        /// Setter for IsInputMethodEnabled AvaloniaProperty
        /// </summary>
        public static void SetIsInputMethodEnabled(InputElement target, bool value)
        {
            target.SetValue(IsInputMethodEnabledProperty, value);
        }

        /// <summary>
        /// Getter for IsInputMethodEnabled AvaloniaProperty
        /// </summary>
        public static bool GetIsInputMethodEnabled(InputElement target)
        {
            return target.GetValue<bool>(IsInputMethodEnabledProperty);
        }

        private InputMethod()
        {

        }
    }
}
