using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
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
    /// A standard button control.
    /// </summary>
    [PseudoClasses(pcFlyoutOpen, pcPressed)]
    public class Button : ContentControl, ICommandSource, IClickableControl
    {
        private const string pcPressed    = ":pressed";
        private const string pcFlyoutOpen = ":flyout-open";

        /// <summary>
        /// Defines the <see cref="ClickMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ClickMode> ClickModeProperty =
            AvaloniaProperty.Register<Button, ClickMode>(nameof(ClickMode));

        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand?> CommandProperty =
            AvaloniaProperty.Register<Button, ICommand?>(nameof(Command), enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="HotKey"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyGesture?> HotKeyProperty =
            HotKeyManager.HotKeyProperty.AddOwner<Button>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            AvaloniaProperty.Register<Button, object?>(nameof(CommandParameter));

        /// <summary>
        /// Defines the <see cref="IsDefault"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDefaultProperty =
            AvaloniaProperty.Register<Button, bool>(nameof(IsDefault));

        /// <summary>
        /// Defines the <see cref="IsCancel"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCancelProperty =
            AvaloniaProperty.Register<Button, bool>(nameof(IsCancel));

        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="IsPressed"/> property.
        /// </summary>
        public static readonly DirectProperty<Button, bool> IsPressedProperty =
            AvaloniaProperty.RegisterDirect<Button, bool>(nameof(IsPressed), b => b.IsPressed);

        /// <summary>
        /// Defines the <see cref="Flyout"/> property
        /// </summary>
        public static readonly StyledProperty<FlyoutBase?> FlyoutProperty =
            AvaloniaProperty.Register<Button, FlyoutBase?>(nameof(Flyout));

        private bool _commandCanExecute = true;
        private KeyGesture? _hotkey;
        private bool _isFlyoutOpen = false;
        private bool _isPressed = false;

        /// <summary>
        /// Initializes static members of the <see cref="Button"/> class.
        /// </summary>
        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<Button>((lbl, args) => lbl.OnAccessKey(args));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Button"/> class.
        /// </summary>
        public Button()
        {
        }

        /// <summary>
        /// Raised when the user clicks the button.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        /// <summary>
        /// Gets or sets a value indicating how the <see cref="Button"/> should react to clicks.
        /// </summary>
        public ClickMode ClickMode
        {
            get => GetValue(ClickModeProperty);
            set => SetValue(ClickModeProperty, value);
        }

        /// <summary>
        /// Gets or sets an <see cref="ICommand"/> to be invoked when the button is clicked.
        /// </summary>
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Gets or sets an <see cref="KeyGesture"/> associated with this control
        /// </summary>
        public KeyGesture? HotKey
        {
            get => GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets a parameter to be passed to the <see cref="Command"/>.
        /// </summary>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is the default button for the
        /// window.
        /// </summary>
        public bool IsDefault
        {
            get => GetValue(IsDefaultProperty);
            set => SetValue(IsDefaultProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is the Cancel button for the
        /// window.
        /// </summary>
        public bool IsCancel
        {
            get => GetValue(IsCancelProperty);
            set => SetValue(IsCancelProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is currently pressed.
        /// </summary>
        public bool IsPressed
        {
            get => _isPressed;
            private set => SetAndRaise(IsPressedProperty, ref _isPressed, value);
        }

        /// <summary>
        /// Gets or sets the Flyout that should be shown with this button.
        /// </summary>
        public FlyoutBase? Flyout
        {
            get => GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        /// <inheritdoc/>
        protected override bool IsEnabledCore => base.IsEnabledCore && _commandCanExecute;

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (IsDefault)
            {
                if (e.Root is IInputElement inputElement)
                {
                    ListenForDefault(inputElement);
                }
            }
            if (IsCancel)
            {
                if (e.Root is IInputElement inputElement)
                {
                    ListenForCancel(inputElement);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (IsDefault)
            {
                if (e.Root is IInputElement inputElement)
                {
                    StopListeningForDefault(inputElement);
                }
            }
            if (IsCancel)
            {
                if (e.Root is IInputElement inputElement)
                {
                    StopListeningForCancel(inputElement);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (_hotkey != null) // Control attached again, set Hotkey to create a hotkey manager for this control
            {
                SetCurrentValue(HotKeyProperty, _hotkey);
            }

            base.OnAttachedToLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged += CanExecuteChanged;
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            // This will cause the hotkey manager to dispose the observer and the reference to this control
            if (HotKey != null)
            {
                _hotkey = HotKey;
                SetCurrentValue(HotKeyProperty, null);
            }

            base.OnDetachedFromLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged -= CanExecuteChanged;
            }
        }

        protected virtual void OnAccessKey(RoutedEventArgs e) => OnClick();

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    OnClick();
                    e.Handled = true;
                    break;

                case Key.Space:
                    {
                        if (ClickMode == ClickMode.Press)
                        {
                            OnClick();
                        }

                        IsPressed = true;
                        e.Handled = true;
                        break;
                    }

                case Key.Escape when Flyout != null:
                    // If Flyout doesn't have focusable content, close the flyout here
                    CloseFlyout();
                    break;
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
                    OnClick();
                }
                IsPressed = false;
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }

        /// <summary>
        /// Invokes the <see cref="Click"/> event.
        /// </summary>
        protected virtual void OnClick()
        {
            if (IsEffectivelyEnabled)
            {
                if (_isFlyoutOpen)
                {
                    CloseFlyout();
                }
                else
                {
                    OpenFlyout();
                }

                var e = new RoutedEventArgs(ClickEvent);
                RaiseEvent(e);

                if (!e.Handled && Command?.CanExecute(CommandParameter) == true)
                {
                    Command.Execute(CommandParameter);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Opens the button's flyout.
        /// </summary>
        protected virtual void OpenFlyout()
        {
            Flyout?.ShowAt(this);
        }

        /// <summary>
        /// Closes the button's flyout.
        /// </summary>
        protected virtual void CloseFlyout()
        {
            Flyout?.Hide();
        }

        /// <summary>
        /// Invoked when the button's flyout is opened.
        /// </summary>
        protected virtual void OnFlyoutOpened()
        {
            // Available for derived types
        }

        /// <summary>
        /// Invoked when the button's flyout is closed.
        /// </summary>
        protected virtual void OnFlyoutClosed()
        {
            // Available for derived types
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                IsPressed = true;
                e.Handled = true;

                if (ClickMode == ClickMode.Press)
                {
                    OnClick();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (IsPressed && e.InitialPressMouseButton == MouseButton.Left)
            {
                IsPressed = false;
                e.Handled = true;

                if (ClickMode == ClickMode.Release &&
                    this.GetVisualsAt(e.GetPosition(this)).Any(c => this == c || this.IsVisualAncestorOf(c)))
                {
                    OnClick();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);

            IsPressed = false;
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            IsPressed = false;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            UnregisterFlyoutEvents(Flyout);
            RegisterFlyoutEvents(Flyout);
            UpdatePseudoClasses();
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CommandProperty)
            {
                if (((ILogical)this).IsAttachedToLogicalTree)
                {
                    var (oldValue, newValue) = change.GetOldAndNewValue<ICommand?>();
                    if (oldValue is ICommand oldCommand)
                    {
                        oldCommand.CanExecuteChanged -= CanExecuteChanged;
                    }

                    if (newValue is ICommand newCommand)
                    {
                        newCommand.CanExecuteChanged += CanExecuteChanged;
                    }
                }

                CanExecuteChanged(this, EventArgs.Empty);
            }
            else if (change.Property == CommandParameterProperty)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
            else if (change.Property == IsCancelProperty)
            {
                var isCancel = change.GetNewValue<bool>();

                if (VisualRoot is IInputElement inputRoot)
                {
                    if (isCancel)
                    {
                        ListenForCancel(inputRoot);
                    }
                    else
                    {
                        StopListeningForCancel(inputRoot);
                    }
                }
            }
            else if (change.Property == IsDefaultProperty)
            {
                var isDefault = change.GetNewValue<bool>();

                if (VisualRoot is IInputElement inputRoot)
                {
                    if (isDefault)
                    {
                        ListenForDefault(inputRoot);
                    }
                    else
                    {
                        StopListeningForDefault(inputRoot);
                    }
                }
            }
            else if (change.Property == IsPressedProperty)
            {
                UpdatePseudoClasses();
            }
            else if (change.Property == FlyoutProperty)
            {
                var (oldFlyout, newFlyout) = change.GetOldAndNewValue<FlyoutBase?>();

                // If flyout is changed while one is already open, make sure we 
                // close the old one first
                if (oldFlyout != null && oldFlyout.IsOpen)
                {
                    oldFlyout.Hide();
                }

                // Must unregister events here while a reference to the old flyout still exists
                UnregisterFlyoutEvents(oldFlyout);

                RegisterFlyoutEvents(newFlyout);
                UpdatePseudoClasses();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ButtonAutomationPeer(this);

        /// <inheritdoc/>
        protected override void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
            base.UpdateDataValidation(property, state, error);
            if (property == CommandProperty)
            {
                if (state == BindingValueType.BindingError)
                {
                    if (_commandCanExecute)
                    {
                        _commandCanExecute = false;
                        UpdateIsEffectivelyEnabled();
                    }
                }
            }
        }

        internal void PerformClick() => OnClick();

        /// <summary>
        /// Called when the <see cref="ICommand.CanExecuteChanged"/> event fires.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void CanExecuteChanged(object? sender, EventArgs e)
        {
            var canExecute = Command == null || Command.CanExecute(CommandParameter);

            if (canExecute != _commandCanExecute)
            {
                _commandCanExecute = canExecute;
                UpdateIsEffectivelyEnabled();
            }
        }

        /// <summary>
        /// Registers all flyout events.
        /// </summary>
        /// <param name="flyout">The flyout to connect events to.</param>
        private void RegisterFlyoutEvents(FlyoutBase? flyout)
        {
            if (flyout != null)
            {
                flyout.Opened += Flyout_Opened;
                flyout.Closed += Flyout_Closed;
            }
        }

        /// <summary>
        /// Explicitly unregisters all flyout events.
        /// </summary>
        /// <param name="flyout">The flyout to disconnect events from.</param>
        private void UnregisterFlyoutEvents(FlyoutBase? flyout)
        {
            if (flyout != null)
            {
                flyout.Opened -= Flyout_Opened;
                flyout.Closed -= Flyout_Closed;
            }
        }

        /// <summary>
        /// Starts listening for the Enter key when the button <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void ListenForDefault(IInputElement root)
        {
            root.AddHandler(KeyDownEvent, RootDefaultKeyDown);
        }

        /// <summary>
        /// Starts listening for the Escape key when the button <see cref="IsCancel"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void ListenForCancel(IInputElement root)
        {
            root.AddHandler(KeyDownEvent, RootCancelKeyDown);
        }

        /// <summary>
        /// Stops listening for the Enter key when the button is no longer <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void StopListeningForDefault(IInputElement root)
        {
            root.RemoveHandler(KeyDownEvent, RootDefaultKeyDown);
        }

        /// <summary>
        /// Stops listening for the Escape key when the button is no longer <see cref="IsCancel"/>.
        /// </summary>
        /// <param name="root">The input root.</param>
        private void StopListeningForCancel(IInputElement root)
        {
            root.RemoveHandler(KeyDownEvent, RootCancelKeyDown);
        }

        /// <summary>
        /// Called when a key is pressed on the input root and the button <see cref="IsDefault"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void RootDefaultKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsVisible && IsEnabled)
            {
                OnClick();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when a key is pressed on the input root and the button <see cref="IsCancel"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void RootCancelKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && IsVisible && IsEnabled)
            {
                OnClick();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Updates the visual state of the control by applying latest PseudoClasses.
        /// </summary>
        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(pcFlyoutOpen, _isFlyoutOpen);
            PseudoClasses.Set(pcPressed, IsPressed);
        }

        void ICommandSource.CanExecuteChanged(object sender, EventArgs e) => this.CanExecuteChanged(sender, e);

        void IClickableControl.RaiseClick() => OnClick();

        /// <summary>
        /// Event handler for when the button's flyout is opened.
        /// </summary>
        private void Flyout_Opened(object? sender, EventArgs e)
        {
            var flyout = sender as FlyoutBase;

            // It is possible to share flyouts among multiple controls including Button.
            // This can cause a problem here since all controls that share a flyout receive
            // the same Opened/Closed events at the same time.
            // For Button that means they all would be updating their pseudoclasses accordingly.
            // In other words, all Buttons with a shared Flyout would have the backgrounds changed together.
            // To fix this, only continue here if the Flyout target matches this Button instance.
            if (object.ReferenceEquals(flyout?.Target, this))
            {
                _isFlyoutOpen = true;
                UpdatePseudoClasses();

                OnFlyoutOpened();
            }
        }

        /// <summary>
        /// Event handler for when the button's flyout is closed.
        /// </summary>
        private void Flyout_Closed(object? sender, EventArgs e)
        {
            var flyout = sender as FlyoutBase;

            // See comments in Flyout_Opened
            if (object.ReferenceEquals(flyout?.Target, this))
            {
                _isFlyoutOpen = false;
                UpdatePseudoClasses();

                OnFlyoutClosed();
            }
        }
    }
}
