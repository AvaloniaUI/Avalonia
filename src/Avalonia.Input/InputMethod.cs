using System;
using Avalonia.Input.TextInput;
using Avalonia.VisualTree;

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
        /// Initializes static members of the <see cref="InputMethod"/> class.
        /// </summary>
        static InputMethod()
        {
            IsInputMethodEnabledProperty.Changed.Subscribe(OnInputMethodEnabledChanged);
        }

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

        private static void OnInputMethodEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> obj)
        {
            if (obj.Sender is InputElement element &&
                ReferenceEquals(element, KeyboardDevice.Instance.FocusedElement))
            {
                (((IVisual)element).VisualRoot as ITextInputMethodRoot)?.InputMethod.SetActive(obj.NewValue.Value);
            }
        }

        private InputMethod()
        {

        }
    }
}
