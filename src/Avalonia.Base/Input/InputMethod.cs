using System;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;

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
        
        /// <summary>
        /// Defines the TextInputMethodClientRequeryRequested event.
        /// </summary>
        public static readonly RoutedEvent<TextInputMethodClientRequeryRequestedEventArgs> TextInputMethodClientRequeryRequestedEvent =
            RoutedEvent.Register<InputElement, TextInputMethodClientRequeryRequestedEventArgs>(
                "TextInputMethodClientRequeryRequested",
                RoutingStrategies.Bubble);
        
        public static void AddTextInputMethodClientRequeryRequestedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(TextInputMethodClientRequeryRequestedEvent, handler);
        }
        
        public static void RemoveTextInputMethodClientRequeryRequestedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(TextInputMethodClientRequeryRequestedEvent, handler);
        }
        
        private InputMethod()
        {

        }
    }
}
