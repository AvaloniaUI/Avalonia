using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public partial class DirectionalPadControl : UserControl
    {

        public static readonly StyledProperty<bool> PadUpProperty =
            AvaloniaProperty.Register<DirectionalPadControl, bool>(nameof(PadUp));
        public bool PadUp
        {
            get => this.GetValue(PadUpProperty);
            set => SetValue(PadUpProperty, value);
        }
        public static readonly StyledProperty<bool> PadDownProperty =
            AvaloniaProperty.Register<DirectionalPadControl, bool>(nameof(PadDown));
        public bool PadDown
        {
            get => this.GetValue(PadDownProperty);
            set => SetValue(PadDownProperty, value);
        }
        public static readonly StyledProperty<bool> PadLeftProperty =
            AvaloniaProperty.Register<DirectionalPadControl, bool>(nameof(PadLeft));
        public bool PadLeft
        {
            get => this.GetValue(PadLeftProperty);
            set => SetValue(PadLeftProperty, value);
        }
        public static readonly StyledProperty<bool> PadRightProperty =
            AvaloniaProperty.Register<DirectionalPadControl, bool>(nameof(PadRight));
        public bool PadRight
        {
            get => this.GetValue(PadRightProperty);
            set => SetValue(PadRightProperty, value);
        }


        public DirectionalPadControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ReceiveUpdate(GamepadUpdateArgs args)
        {
            var state = args.State;
            PadUp = state.GetButtonState(GamepadButton.Button12).Pressed;
            PadDown = state.GetButtonState(GamepadButton.Button13).Pressed;
            PadLeft = state.GetButtonState(GamepadButton.Button14).Pressed;
            PadRight = state.GetButtonState(GamepadButton.Button15).Pressed;
        }
    }
}
