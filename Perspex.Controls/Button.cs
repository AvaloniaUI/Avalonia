// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using System.Windows.Input;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Rendering;
    using Perspex.VisualTree;

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

        public static readonly PerspexProperty<bool> IsDefaultProperty =
            PerspexProperty.Register<Button, bool>("IsDefault");

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>("Click", RoutingStrategies.Bubble);

        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
            ClickEvent.AddClassHandler<Button>(x => x.OnClick);
            CommandProperty.Changed.Subscribe(CommandChanged);
            IsDefaultProperty.Changed.Subscribe(IsDefaultChanged);
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

        public bool IsDefault
        {
            get { return this.GetValue(IsDefaultProperty); }
            set { this.SetValue(IsDefaultProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            if (this.IsDefault)
            {
                var inputElement = root as IInputElement;

                if (inputElement != null)
                {
                    this.ListenForDefault(inputElement);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.RaiseClickEvent();
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (this.ClickMode == ClickMode.Press)
                {
                    this.RaiseClickEvent();
                }

                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (this.ClickMode == ClickMode.Release)
                {
                    this.RaiseClickEvent();
                }

                e.Handled = true;
            }
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);

            if (this.IsDefault)
            {
                var inputElement = oldRoot as IInputElement;

                if (inputElement != null)
                {
                    this.StopListeningForDefault(inputElement);
                }
            }
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
                    newCommand.CanExecuteChanged += button.CanExecuteChanged;
                }

                button.CanExecuteChanged(button, EventArgs.Empty);
            }
        }

        private static void IsDefaultChanged(PerspexPropertyChangedEventArgs e)
        {
            var button = e.Sender as Button;
            var isDefault = (bool)e.NewValue;
            var root = button.GetSelfAndVisualAncestors().OfType<IRenderRoot>().FirstOrDefault();
            var inputElement = root as IInputElement;

            if (inputElement != null)
            {
                if (isDefault)
                {
                    button.ListenForDefault(inputElement);
                }
                else
                {
                    button.StopListeningForDefault(inputElement);
                }
            }
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            // HACK: Just set the IsEnabled property for the moment. This needs to be changed to
            // use IsEnabledCore etc. but it will do for now.
            this.IsEnabled = this.Command == null || this.Command.CanExecute(this.CommandParameter);
        }

        private void ListenForDefault(IInputElement root)
        {
            root.AddHandler(InputElement.KeyDownEvent, this.RootKeyDown);
        }

        private void StopListeningForDefault(IInputElement root)
        {
            root.RemoveHandler(InputElement.KeyDownEvent, this.RootKeyDown);
        }

        private void RaiseClickEvent()
        {
            RoutedEventArgs click = new RoutedEventArgs
            {
                RoutedEvent = ClickEvent,
            };

            this.RaiseEvent(click);
        }

        private void RootKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.IsVisible && this.IsEnabled)
            {
                this.RaiseClickEvent();
            }
        }
    }
}
