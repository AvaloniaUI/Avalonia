using System;
using System.Windows.Input;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A button with primary and secondary parts that can each be pressed separately.
    /// The primary part behaves like a <see cref="Button"/> and the secondary part opens a flyout.
    /// </summary>
    [TemplatePart("PART_PrimaryButton",   typeof(Button))]
    [TemplatePart("PART_SecondaryButton", typeof(Button))]
    [PseudoClasses(pcFlyoutOpen, pcPressed)]
    public class SplitButton : ContentControl, ICommandSource
    {
        internal const string pcChecked    = ":checked";
        internal const string pcPressed    = ":pressed";
        internal const string pcFlyoutOpen = ":flyout-open";

        /// <summary>
        /// Raised when the user presses the primary part of the <see cref="SplitButton"/>.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<SplitButton, RoutedEventArgs>(
                nameof(Click),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand?> CommandProperty =
            Button.CommandProperty.AddOwner<SplitButton>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<SplitButton>();

        /// <summary>
        /// Defines the <see cref="Flyout"/> property
        /// </summary>
        public static readonly StyledProperty<FlyoutBase?> FlyoutProperty =
            Button.FlyoutProperty.AddOwner<SplitButton>();

        private Button? _primaryButton   = null;
        private Button? _secondaryButton = null;

        private bool _commandCanExecute       = true;
        private bool _isAttachedToLogicalTree = false;
        private bool _isFlyoutOpen            = false;
        private bool _isKeyboardPressed       = false;

        private IDisposable? _flyoutPropertyChangedDisposable;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitButton"/> class.
        /// </summary>
        public SplitButton()
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> invoked when the primary part is pressed.
        /// </summary>
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
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
        /// Gets or sets the <see cref="FlyoutBase"/> that is shown when the secondary part is pressed.
        /// </summary>
        public FlyoutBase? Flyout
        {
            get => GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the button is currently checked.
        /// </summary>
        /// <remarks>
        /// This property exists only for the derived <see cref="ToggleSplitButton"/> and is
        /// unused (set to false) within <see cref="SplitButton"/>. Doing this allows the
        /// two controls to share a default style.
        /// </remarks>
        internal virtual bool InternalIsChecked => false;

        /// <inheritdoc/>
        protected override bool IsEnabledCore => base.IsEnabledCore && _commandCanExecute;

        /// <inheritdoc/>
        void ICommandSource.CanExecuteChanged(object sender, EventArgs e) => this.CanExecuteChanged(sender, e);

        /// <inheritdoc cref="ICommandSource.CanExecuteChanged"/>
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
        /// Updates the visual state of the control by applying latest PseudoClasses.
        /// </summary>
        protected void UpdatePseudoClasses()
        {
            PseudoClasses.Set(pcFlyoutOpen, _isFlyoutOpen);
            PseudoClasses.Set(pcPressed, _isKeyboardPressed);
            PseudoClasses.Set(pcChecked, InternalIsChecked);
        }

        /// <summary>
        /// Opens the secondary button's flyout.
        /// </summary>
        protected void OpenFlyout()
        {
            Flyout?.ShowAt(this);
        }

        /// <summary>
        /// Closes the secondary button's flyout.
        /// </summary>
        protected void CloseFlyout()
        {
            Flyout?.Hide();
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

                _flyoutPropertyChangedDisposable = flyout.GetPropertyChangedObservable(Popup.PlacementProperty).Subscribe(Flyout_PlacementPropertyChanged);
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

                _flyoutPropertyChangedDisposable?.Dispose();
                _flyoutPropertyChangedDisposable = null;
             }
        }

        /// <summary>
        /// Explicitly unregisters all events related to the two buttons in OnApplyTemplate().
        /// </summary>
        private void UnregisterEvents()
        {
            if (_primaryButton != null)
            {
                _primaryButton.Click -= PrimaryButton_Click;
            }

            if (_secondaryButton != null)
            {
                _secondaryButton.Click -= SecondaryButton_Click;
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            UnregisterEvents();
            UnregisterFlyoutEvents(Flyout);

            _primaryButton   = e.NameScope.Find<Button>("PART_PrimaryButton");
            _secondaryButton = e.NameScope.Find<Button>("PART_SecondaryButton");

            if (_primaryButton != null)
            {
                _primaryButton.Click += PrimaryButton_Click;
            }

            if (_secondaryButton != null)
            {
                _secondaryButton.Click += SecondaryButton_Click;
            }

            RegisterFlyoutEvents(Flyout);
            UpdatePseudoClasses();
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged += CanExecuteChanged;
                CanExecuteChanged(this, EventArgs.Empty);
            }

            _isAttachedToLogicalTree = true;
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged -= CanExecuteChanged;
            }

            _isAttachedToLogicalTree = false;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CommandProperty)
            {
                if (_isAttachedToLogicalTree)
                {
                    // Must unregister events here while a reference to the old command still exists
                    var (oldValue, newValue) = e.GetOldAndNewValue<ICommand?>();

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
            else if (e.Property == CommandParameterProperty)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
            else if (e.Property == FlyoutProperty)
            {
                var (oldFlyout, newFlyout) = e.GetOldAndNewValue<FlyoutBase?>();

                // If flyout is changed while one is already open, make sure we 
                // close the old one first
                // This is the same behavior as Button
                if (oldFlyout != null &&
                    oldFlyout.IsOpen)
                {
                    oldFlyout.Hide();
                }

                // Must unregister events here while a reference to the old flyout still exists
                UnregisterFlyoutEvents(oldFlyout);

                RegisterFlyoutEvents(newFlyout);
                UpdatePseudoClasses();
            }

            base.OnPropertyChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var key = e.Key;

            if (key == Key.Space || key == Key.Enter)
            {
                _isKeyboardPressed = true;
                UpdatePseudoClasses();
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            var key = e.Key;

            if (key == Key.Space || key == Key.Enter)
            {
                _isKeyboardPressed = false;
                UpdatePseudoClasses();

                // Consider this a click on the primary button
                if (IsEffectivelyEnabled)
                {
                    OnClickPrimary(null);
                    e.Handled = true;
                }
            }
            else if (key == Key.Down && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) && IsEffectivelyEnabled
                     && !XYFocusHelpers.IsAllowedXYNavigationMode(this, e.KeyDeviceType))
            {
                OpenFlyout();
                e.Handled = true;
            }
            else if (key == Key.F4 && IsEffectivelyEnabled)
            {
                OpenFlyout();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && _isFlyoutOpen)
            {
                // If Flyout doesn't have focusable content, close the flyout here
                // This is the same behavior as Button
                CloseFlyout();
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }

        /// <summary>
        /// Invokes the <see cref="Click"/> event when the primary button part is clicked.
        /// </summary>
        /// <param name="e">The event args from the internal Click event.</param>
        protected virtual void OnClickPrimary(RoutedEventArgs? e)
        {
            // Note: It is not currently required to check enabled status; however, this is a failsafe
            if (IsEffectivelyEnabled)
            {
                var eventArgs = new RoutedEventArgs(ClickEvent);
                RaiseEvent(eventArgs);

                if (!eventArgs.Handled && Command?.CanExecute(CommandParameter) == true)
                {
                    Command.Execute(CommandParameter);
                    eventArgs.Handled = true;
                }
            }
        }

        /// <summary>
        /// Invoked when the secondary button part is clicked.
        /// </summary>
        /// <param name="e">The event args from the internal Click event.</param>
        protected virtual void OnClickSecondary(RoutedEventArgs? e)
        {
            // Note: It is not currently required to check enabled status; however, this is a failsafe
            if (IsEffectivelyEnabled)
            {
                OpenFlyout();
            }
        }

        /// <summary>
        /// Invoked when the split button's flyout is opened.
        /// </summary>
        protected virtual void OnFlyoutOpened()
        {
            // Available for derived types
        }

        /// <summary>
        /// Invoked when the split button's flyout is closed.
        /// </summary>
        protected virtual void OnFlyoutClosed()
        {
            // Available for derived types
        }

        /// <summary>
        /// Event handler for when the internal primary button part is pressed.
        /// </summary>
        private void PrimaryButton_Click(object? sender, RoutedEventArgs e)
        {
            // Handle internal button click, so it won't bubble outside together with SplitButton.ClickEvent.
            e.Handled = true;
            OnClickPrimary(e);
        }

        /// <summary>
        /// Event handler for when the internal secondary button part is pressed.
        /// </summary>
        private void SecondaryButton_Click(object? sender, RoutedEventArgs e)
        {
            // Handle internal button click, so it won't bubble outside.
            e.Handled = true;
            OnClickSecondary(e);
        }

        /// <summary>
        /// Called when the <see cref="PopupFlyoutBase.Placement"/> property changes.
        /// </summary>
        private void Flyout_PlacementPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Event handler for when the split button's flyout is opened.
        /// </summary>
        private void Flyout_Opened(object? sender, EventArgs e)
        {
            var flyout = sender as FlyoutBase;

            // It is possible to share flyouts among multiple controls including SplitButton.
            // This can cause a problem here since all controls that share a flyout receive
            // the same Opened/Closed events at the same time.
            // For SplitButton that means they all would be updating their pseudoclasses accordingly.
            // In other words, all SplitButtons with a shared Flyout would have the backgrounds changed together.
            // To fix this, only continue here if the Flyout target matches this SplitButton instance.
            if (object.ReferenceEquals(flyout?.Target, this))
            {
                _isFlyoutOpen = true;
                UpdatePseudoClasses();

                OnFlyoutOpened();
            }
        }

        /// <summary>
        /// Event handler for when the split button's flyout is closed.
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
