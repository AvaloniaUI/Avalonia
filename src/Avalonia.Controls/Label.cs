using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Label control. Focuses <see cref="Target"/> on pointer click or access key press (Alt + accessKey)
    /// </summary>
    public class Label : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="Target"/> Direct property
        /// </summary>
        public static readonly StyledProperty<IInputElement?> TargetProperty =
            AvaloniaProperty.Register<Label, IInputElement?>(nameof(Target));

        /// <summary>
        /// Label focus Target
        /// </summary>
        [ResolveByName]
        public IInputElement? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        static Label()
        {
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<Label>((lbl, args) => lbl.LabelActivated(args));
            // IsTabStopProperty.OverrideDefaultValue<Label>(false)
            
            // Set the default value to true, to ensure that the target control can be focused,
            // when the label is focused. 
            FocusableProperty.OverrideDefaultValue<Label>(true);
        }

        /// <summary>
        /// Initializes instance of <see cref="Label"/> control
        /// </summary>
        public Label()
        {
        }

        /// <summary>
        /// Method which focuses <see cref="Target"/> input element
        /// </summary>
        private void LabelActivated(RoutedEventArgs args)
        {
            Target?.Focus();
            args.Handled = Target != null;
        }

        /// <summary>
        /// Handler of <see cref="IInputElement.PointerPressed"/> event
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                LabelActivated(e);
            }
            base.OnPointerPressed(e);
        }

        /// <summary>
        /// Focus the target control instead if the label gets the focus. 
        /// </summary>
        /// <param name="e">The event args</param>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            Target?.Focus();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new LabelAutomationPeer(this);
        }
    }
}
