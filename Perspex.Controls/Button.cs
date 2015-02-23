// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Windows.Input;
    using Perspex.Input;
    using Perspex.Interactivity;

    public enum ClickMode
    {
        Release,
        Press,
    }

    public class Button : ContentControl
    {
        public static readonly PerspexProperty<ClickMode> ClickModeProperty =
            PerspexProperty.Register<Button, ClickMode>("ClickMode");

        public static readonly PerspexProperty<ICommand> CommandProperty =
            PerspexProperty.Register<Button, ICommand>("Command");

        public static readonly PerspexProperty<object> CommandParameterProperty =
            PerspexProperty.Register<Button, object>("CommandParameter");

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>("Click", RoutingStrategies.Bubble);

        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
            ClickEvent.AddClassHandler<Button>(x => x.OnClick);
            CommandProperty.Changed.Subscribe(CommandChanged);
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add { this.AddHandler(ClickEvent, value); }
            remove { this.RemoveHandler(ClickEvent, value); }
        }

        public ClickMode ClickMode
        {
            get { return this.GetValue(ClickModeProperty); }
            set { this.SetValue(ClickModeProperty, value); }
        }

        public ICommand Command
        {
            get { return this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        protected virtual void OnClick(RoutedEventArgs e)
        {
            if (this.Command != null)
            {
                this.Command.Execute(this.CommandParameter);
            }
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);

            this.Classes.Add(":pressed");
            e.Device.Capture(this);
            e.Handled = true;

            if (this.ClickMode == ClickMode.Press)
            {
                this.RaiseClickEvent();
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            base.OnPointerReleased(e);

            e.Device.Capture(null);
            this.Classes.Remove(":pressed");
            e.Handled = true;

            if (this.ClickMode == ClickMode.Release && this.Classes.Contains(":pointerover"))
            {
                this.RaiseClickEvent();
            }
        }

        private static void CommandChanged(PerspexPropertyChangedEventArgs e)
        {
            var button = e.Sender as Button;

            if (button != null)
            {
                var oldCommand = e.OldValue as ICommand;
                var newCommand = e.NewValue as ICommand;

                if (oldCommand != null)
                {
                    oldCommand.CanExecuteChanged -= button.CanExecuteChanged;
                }

                if (newCommand != null)
                {
                    newCommand.CanExecuteChanged -= button.CanExecuteChanged;
                }

                button.CanExecuteChanged(button, EventArgs.Empty);
            }
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            // HACK: Just set the IsEnabled property for the moment. This needs to be changed to 
            // use IsEnabledCore etc. but it will do for now.
            this.IsEnabled = this.Command == null || this.Command.CanExecute(this.CommandParameter);
        }

        private void RaiseClickEvent()
        {
            RoutedEventArgs click = new RoutedEventArgs
            {
                RoutedEvent = ClickEvent,
            };

            this.RaiseEvent(click);
        }
    }
}
