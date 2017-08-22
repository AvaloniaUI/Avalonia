// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines how a <see cref="Button"/> reacts to clicks.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>
        /// The <see cref="Button.Click"/> event is raised when the pointer is released.
        /// </summary>
        Release,

        /// <summary>
        /// The <see cref="Button.Click"/> event is raised when the pointer is pressed.
        /// </summary>
        Press,
    }

    /// <summary>
    /// A button control.
    /// </summary>
    public class Button : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="ClickMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ClickMode> ClickModeProperty =
            AvaloniaProperty.Register<Button, ClickMode>(nameof(ClickMode));

        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly DirectProperty<Button, ICommand> CommandProperty =
            AvaloniaProperty.RegisterDirect<Button, ICommand>(nameof(Command),
                button => button.Command, (button, command) => button.Command = command, enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="HotKey"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyGesture> HotKeyProperty =
            HotKeyManager.HotKeyProperty.AddOwner<Button>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object> CommandParameterProperty =
            AvaloniaProperty.Register<Button, object>(nameof(CommandParameter));

        /// <summary>
        /// Defines the <see cref="IsDefaultProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDefaultProperty =
            AvaloniaProperty.Register<Button, bool>(nameof(IsDefault));

        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>("Click", RoutingStrategies.Bubble);

        private ICommand _command;

        public static readonly AvaloniaProperty<bool> IsPressedProperty =
            AvaloniaProperty.RegisterDirect<Button, bool>(nameof(IsPressed), b => b.IsPressed);

        /// <summary>
        /// Initializes static members of the <see cref="Button"/> class.
        /// </summary>
        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
            ClickEvent.AddClassHandler<Button>(x => x.OnClick);
            CommandProperty.Changed.Subscribe(CommandChanged);
            IsDefaultProperty.Changed.Subscribe(IsDefaultChanged);
        }

        /// <summary>
        /// Raised when the user clicks the button.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating how the <see cref="Button"/> should react to clicks.
        /// </summary>
        public ClickMode ClickMode
        {
            get { return GetValue(ClickModeProperty); }
            set { SetValue(ClickModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets an <see cref="ICommand"/> to be invoked when the button is clicked.
        /// </summary>
        public ICommand Command
        {
            get { return _command; }
            set { SetAndRaise(CommandProperty, ref _command, value); }
        }

        /// <summary>
        /// Gets or sets an <see cref="KeyGesture"/> associated with this control
        /// </summary>
        public KeyGesture HotKey
        {
            get { return GetValue(HotKeyProperty); }
            set { SetValue(HotKeyProperty, value); }
        }

        /// <summary>
        /// Gets or sets a parameter to be passed to the <see cref="Command"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is the default button for the
        /// window.
        /// </summary>
        public bool IsDefault
        {
            get { return GetValue(IsDefaultProperty); }
            set { SetValue(IsDefaultProperty, value); }
        }

        public bool IsPressed
        {
            get { return GetValue(IsPressedProperty); }
            private set { SetValue(IsPressedProperty, value); }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (IsDefault)
            {
                var inputElement = e.Root as IInputElement;

                if (inputElement != null)
                {
                    ListenForDefault(inputElement);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RaiseClickEvent();
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (ClickMode == ClickMode.Press)
                {
                    RaiseClickEvent();
                }
                IsPressed = true;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (ClickMode == ClickMode.Release)
                {
                    RaiseClickEvent();
                }
                IsPressed = false;
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (IsDefault)
            {
                var inputElement = e.Root as IInputElement;

                if (inputElement != null)
                {
                    StopListeningForDefault(inputElement);
                }
            }
        }

        /// <summary>
        /// Invokes the <see cref="Click"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnClick(RoutedEventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(CommandParameter);
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.MouseButton == MouseButton.Left)
            {
                PseudoClasses.Add(":pressed");
                e.Device.Capture(this);
                IsPressed = true;
                e.Handled = true;

                if (ClickMode == ClickMode.Press)
                {
                    RaiseClickEvent();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (e.MouseButton == MouseButton.Left)
            {
                e.Device.Capture(null);
                PseudoClasses.Remove(":pressed");
                IsPressed = false;
                e.Handled = true;

                if (ClickMode == ClickMode.Release && new Rect(Bounds.Size).Contains(e.GetPosition(this)))
                {
                    RaiseClickEvent();
                }
            }
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification status)
        {
            base.UpdateDataValidation(property, status);
            if(property == CommandProperty)
            {
                if(status?.ErrorType == BindingErrorType.Error)
                {
                    IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Command"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void CommandChanged(AvaloniaPropertyChangedEventArgs e)
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

        /// <summary>
        /// Called when the <see cref="IsDefault"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void IsDefaultChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var button = e.Sender as Button;
            var isDefault = (bool)e.NewValue;
            var inputRoot = button?.VisualRoot as IInputElement;

            if (inputRoot != null)
            {
                if (isDefault)
                {
                    button.ListenForDefault(inputRoot);
                }
                else
                {
                    button.StopListeningForDefault(inputRoot);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="ICommand.CanExecuteChanged"/> event fires.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void CanExecuteChanged(object sender, EventArgs e)
        {
            // HACK: Just set the IsEnabled property for the moment. This needs to be changed to
            // use IsEnabledCore etc. but it will do for now.
            IsEnabled = Command == null || Command.CanExecute(CommandParameter);
        }

        /// <summary>
        /// Starts listening for the Enter key when the button <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void ListenForDefault(IInputElement root)
        {
            root.AddHandler(KeyDownEvent, RootKeyDown);
        }

        /// <summary>
        /// Stops listening for the Enter key when the button is no longer <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void StopListeningForDefault(IInputElement root)
        {
            root.RemoveHandler(KeyDownEvent, RootKeyDown);
        }

        /// <summary>
        /// Raises the <see cref="Click"/> event.
        /// </summary>
        private void RaiseClickEvent()
        {
            RoutedEventArgs click = new RoutedEventArgs
            {
                RoutedEvent = ClickEvent,
            };

            RaiseEvent(click);
        }

        /// <summary>
        /// Called when a key is pressed on the input root and the button <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void RootKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsVisible && IsEnabled)
            {
                RaiseClickEvent();
            }
        }
    }
}
