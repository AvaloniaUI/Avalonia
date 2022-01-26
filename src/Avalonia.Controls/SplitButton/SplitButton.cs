using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A button with primary and secondary parts that can each be pressed separately.
    /// The primary part behaves like a <see cref="Button"/> and the secondary part opens a flyout.
    /// </summary>
    [PseudoClasses(
        pcDisabled,
        pcSecondaryButtonRight,
        pcSecondaryButtonSpan,
        pcCheckedFlyoutOpen,
        pcFlyoutOpen,
        pcCheckedTouchPressed,
        pcChecked,
        pcCheckedPrimaryPressed,
        pcCheckedPrimaryPointerOver,
        pcCheckedSecondaryPressed,
        pcCheckedSecondaryPointerOver,
        pcTouchPressed,
        pcPrimaryPressed,
        pcPrimaryPointerOver,
        pcSecondaryPressed,
        pcSecondaryPointerOver)]
    public class SplitButton : ContentControl, ICommandSource
    {
        private const string pcDisabled = ":disabled";

        private const string pcSecondaryButtonRight = ":secondary-button-right";
        private const string pcSecondaryButtonSpan  = ":secondary-button-span";

        private const string pcCheckedFlyoutOpen = ":checked-flyout-open";
        private const string pcFlyoutOpen        = ":flyout-open";

        private const string pcCheckedTouchPressed         = ":checked-touch-pressed";
        private const string pcChecked                     = ":checked";
        private const string pcCheckedPrimaryPressed       = ":checked-primary-pressed";
        private const string pcCheckedPrimaryPointerOver   = ":checked-primary-pointerover";
        private const string pcCheckedSecondaryPressed     = ":checked-secondary-pressed";
        private const string pcCheckedSecondaryPointerOver = ":checked-secondary-pointerover";

        private const string pcTouchPressed         = ":touch-pressed";
        private const string pcPrimaryPressed       = ":primary-pressed";
        private const string pcPrimaryPointerOver   = ":primary-pointerover";
        private const string pcSecondaryPressed     = ":secondary-pressed";
        private const string pcSecondaryPointerOver = ":secondary-pointerover";

        /// <summary>
        /// Raised when the user presses the primary part of the <see cref="SplitButton"/>.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Click
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
        public static readonly DirectProperty<SplitButton, ICommand> CommandProperty =
            AvaloniaProperty.RegisterDirect<SplitButton, ICommand>(
                nameof(Command),
                splitButton => splitButton.Command,
                (splitButton, command) => splitButton.Command = command,
                enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object> CommandParameterProperty =
            AvaloniaProperty.Register<SplitButton, object>(
                nameof(CommandParameter));

        /// <summary>
        /// Defines the <see cref="Flyout"/> property
        /// </summary>
        public static readonly StyledProperty<FlyoutBase> FlyoutProperty =
            AvaloniaProperty.Register<SplitButton, FlyoutBase>(
                nameof(Flyout));

        private ICommand _Command;

        private Button _primaryButton   = null;
        private Button _secondaryButton = null;

        private bool        _commandCanExecute       = true;
        protected bool      _hasLoaded               = false;
        private bool        _isAttachedToLogicalTree = false;
        private bool        _isFlyoutOpen            = false;
        private bool        _isKeyDown               = false;
        private PointerType _lastPointerType         = PointerType.Mouse;

        private CompositeDisposable _buttonPropertyChangedDisposable;
        private IDisposable         _flyoutPropertyChangedDisposable;

        ////////////////////////////////////////////////////////////////////////
        // Constructor / Destructors
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitButton"/> class.
        /// </summary>
        public SplitButton()
        {
        }

        ////////////////////////////////////////////////////////////////////////
        // Properties
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> invoked when the primary part is pressed.
        /// </summary>
        public ICommand Command
        {
            get => _Command;
            set => SetAndRaise(CommandProperty, ref _Command, value);
        }

        /// <summary>
        /// Gets or sets a parameter to be passed to the <see cref="Command"/>.
        /// </summary>
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="FlyoutBase"/> that is shown when the secondary part is pressed.
        /// </summary>
        public FlyoutBase Flyout
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

        ////////////////////////////////////////////////////////////////////////
        // Methods
        ////////////////////////////////////////////////////////////////////////

        /// <inheritdoc/>
        public void CanExecuteChanged(object sender, EventArgs e)
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
            // Place the secondary button
            //
            // In WinUI, the span of the secondary button is changed to full-width for touch-based
            // devices in certain conditions. The full reasoning for this is unknown. Some theories
            // include:
            //
            //   My guess is that the design team at MS decided that it's a better experience
            //   for touch users to make them select from the drop down rather than the shortcut
            //   top level button, so touch basically just turns this into a drop down button.
            //   Whether that's ideal or not is going is a subjective opinion.
            //
            // For Avalonia, it may not always make sense to disable the primary button like that
            // on touch-first platforms. Users and developers would normally expect a control to
            // function the same on all platforms. Therefore, this functionality is disabled here
            // but could be re-enabled in the future if more reasons become known.
            //
            // Finally, these are mutually exclusive PseudoClasses handled separately from
            // SetExclusivePseudoClass(). They must be applied in addition to the others.
            /*
            if (_lastPointerType == PointerType.Touch || _isKeyDown)
            {
                PseudoClasses.Set(pcSecondaryButtonSpan, true);
                PseudoClasses.Set(pcSecondaryButtonRight, false);
            }
            else
            {
                PseudoClasses.Set(pcSecondaryButtonSpan, false);
                PseudoClasses.Set(pcSecondaryButtonRight, true);
            }
            */

            // Change the visual state
            if (!IsEffectivelyEnabled)
            {
                SetExclusivePseudoClass(pcDisabled);
            }
            else if (_primaryButton != null && _secondaryButton != null)
            {
                if (_isFlyoutOpen)
                {
                    if (InternalIsChecked)
                    {
                        SetExclusivePseudoClass(pcCheckedFlyoutOpen);
                    }
                    else
                    {
                        SetExclusivePseudoClass(pcFlyoutOpen);
                    }
                }
                // SplitButton and ToggleSplitButton share a template -- this section is driving the checked states for ToggleSplitButton.
                else if (InternalIsChecked)
                {
                    if (_lastPointerType == PointerType.Touch || _isKeyDown)
                    {
                        if (_primaryButton.IsPressed || _secondaryButton.IsPressed || _isKeyDown)
                        {
                            SetExclusivePseudoClass(pcCheckedTouchPressed);
                        }
                        else
                        {
                            SetExclusivePseudoClass(pcChecked);
                        }
                    }
                    else if (_primaryButton.IsPressed)
                    {
                        SetExclusivePseudoClass(pcCheckedPrimaryPressed);
                    }
                    else if (_primaryButton.IsPointerOver)
                    {
                        SetExclusivePseudoClass(pcCheckedPrimaryPointerOver);
                    }
                    else if (_secondaryButton.IsPressed)
                    {
                        SetExclusivePseudoClass(pcCheckedSecondaryPressed);
                    }
                    else if (_secondaryButton.IsPointerOver)
                    {
                        SetExclusivePseudoClass(pcCheckedSecondaryPointerOver);
                    }
                    else
                    {
                        SetExclusivePseudoClass(pcChecked);
                    }
                }
                else
                {
                    if (_lastPointerType == PointerType.Touch || _isKeyDown)
                    {
                        if (_primaryButton.IsPressed || _secondaryButton.IsPressed || _isKeyDown)
                        {
                            SetExclusivePseudoClass(pcTouchPressed);
                        }
                        else
                        {
                            // Calling without a parameter is treated as ':normal' and will clear all other
                            // PseudoClasses returning to the default state
                            SetExclusivePseudoClass();
                        }
                    }
                    else if (_primaryButton.IsPressed)
                    {
                        SetExclusivePseudoClass(pcPrimaryPressed);
                    }
                    else if (_primaryButton.IsPointerOver)
                    {
                        SetExclusivePseudoClass(pcPrimaryPointerOver);
                    }
                    else if (_secondaryButton.IsPressed)
                    {
                        SetExclusivePseudoClass(pcSecondaryPressed);
                    }
                    else if (_secondaryButton.IsPointerOver)
                    {
                        SetExclusivePseudoClass(pcSecondaryPointerOver);
                    }
                    else
                    {
                        // Calling without a parameter is treated as ':normal' and will clear all other
                        // PseudoClasses returning to the default state
                        SetExclusivePseudoClass();
                    }
                }
            }

            // Local function to enable the specified PseudoClass and disable all others
            // This more closely matches the VisualStateManager of WinUI where the default style originated
            void SetExclusivePseudoClass(string pseudoClass = "")
            {
                PseudoClasses.Set(pcDisabled, pseudoClass == pcDisabled);

                PseudoClasses.Set(pcCheckedFlyoutOpen, pseudoClass == pcCheckedFlyoutOpen);
                PseudoClasses.Set(pcFlyoutOpen,        pseudoClass == pcFlyoutOpen);

                PseudoClasses.Set(pcCheckedTouchPressed,         pseudoClass == pcCheckedTouchPressed);
                PseudoClasses.Set(pcChecked,                     pseudoClass == pcChecked);
                PseudoClasses.Set(pcCheckedPrimaryPressed,       pseudoClass == pcCheckedPrimaryPressed);
                PseudoClasses.Set(pcCheckedPrimaryPointerOver,   pseudoClass == pcCheckedPrimaryPointerOver);
                PseudoClasses.Set(pcCheckedSecondaryPressed,     pseudoClass == pcCheckedSecondaryPressed);
                PseudoClasses.Set(pcCheckedSecondaryPointerOver, pseudoClass == pcCheckedSecondaryPointerOver);

                PseudoClasses.Set(pcTouchPressed,         pseudoClass == pcTouchPressed);
                PseudoClasses.Set(pcPrimaryPressed,       pseudoClass == pcPrimaryPressed);
                PseudoClasses.Set(pcPrimaryPointerOver,   pseudoClass == pcPrimaryPointerOver);
                PseudoClasses.Set(pcSecondaryPressed,     pseudoClass == pcSecondaryPressed);
                PseudoClasses.Set(pcSecondaryPointerOver, pseudoClass == pcSecondaryPointerOver);
            }
        }

        /// <summary>
        /// Opens the secondary button's flyout.
        /// </summary>
        protected void OpenFlyout()
        {
            if (Flyout != null)
            {
                Flyout.ShowAt(this);
            }
        }

        /// <summary>
        /// Closes the secondary button's flyout.
        /// </summary>
        protected void CloseFlyout()
        {
            if (Flyout != null)
            {
                Flyout.Hide();
            }
        }

        /// <summary>
        /// Registers all flyout events.
        /// </summary>
        /// <param name="flyout">The flyout to connect events to.</param>
        private void RegisterFlyoutEvents(FlyoutBase flyout)
        {
            if (flyout != null)
            {
                flyout.Opened += Flyout_Opened;
                flyout.Closed += Flyout_Closed;

                _flyoutPropertyChangedDisposable = flyout.GetPropertyChangedObservable(FlyoutBase.PlacementProperty).Subscribe(Flyout_PlacementPropertyChanged);
            }
        }

        /// <summary>
        /// Explicitly unregisters all flyout events.
        /// </summary>
        /// <param name="flyout">The flyout to disconnect events from.</param>
        private void UnregisterFlyoutEvents(FlyoutBase flyout)
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
            _buttonPropertyChangedDisposable?.Dispose();
            _buttonPropertyChangedDisposable = null;

            if (_primaryButton != null)
            {
                _primaryButton.Click -= PrimaryButton_Click;

                _primaryButton.PointerEnter       -= Button_PointerEvent;
                _primaryButton.PointerLeave       -= Button_PointerEvent;
                _primaryButton.PointerPressed     -= Button_PointerEvent;
                _primaryButton.PointerReleased    -= Button_PointerEvent;
                _primaryButton.PointerCaptureLost -= Button_PointerCaptureLost;
            }

            if (_secondaryButton != null)
            {
                _secondaryButton.Click -= SecondaryButton_Click;

                _secondaryButton.PointerEnter       -= Button_PointerEvent;
                _secondaryButton.PointerLeave       -= Button_PointerEvent;
                _secondaryButton.PointerPressed     -= Button_PointerEvent;
                _secondaryButton.PointerReleased    -= Button_PointerEvent;
                _secondaryButton.PointerCaptureLost -= Button_PointerCaptureLost;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // OnEvent Overridable Methods
        ////////////////////////////////////////////////////////////////////////

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            UnregisterEvents();
            UnregisterFlyoutEvents(Flyout);

            _primaryButton   = e.NameScope.Find<Button>("PART_PrimaryButton");
            _secondaryButton = e.NameScope.Find<Button>("PART_SecondaryButton");

            _buttonPropertyChangedDisposable = new CompositeDisposable();

            if (_primaryButton != null)
            {
                _primaryButton.Click += PrimaryButton_Click;

                _buttonPropertyChangedDisposable.Add(_primaryButton.GetPropertyChangedObservable(Button.IsPressedProperty).Subscribe(Button_VisualPropertyChanged));
                _buttonPropertyChangedDisposable.Add(_primaryButton.GetPropertyChangedObservable(Button.IsPointerOverProperty).Subscribe(Button_VisualPropertyChanged));

                // Register for pointer events to keep track of the last used pointer type and update visual states
                _primaryButton.PointerEnter       += Button_PointerEvent;
                _primaryButton.PointerLeave       += Button_PointerEvent;
                _primaryButton.PointerPressed     += Button_PointerEvent;
                _primaryButton.PointerReleased    += Button_PointerEvent;
                _primaryButton.PointerCaptureLost += Button_PointerCaptureLost;
            }

            if (_secondaryButton != null)
            {
                _secondaryButton.Click += SecondaryButton_Click;

                _buttonPropertyChangedDisposable.Add(_secondaryButton.GetPropertyChangedObservable(Button.IsPressedProperty).Subscribe(Button_VisualPropertyChanged));
                _buttonPropertyChangedDisposable.Add(_secondaryButton.GetPropertyChangedObservable(Button.IsPointerOverProperty).Subscribe(Button_VisualPropertyChanged));

                // Register for pointer events to keep track of the last used pointer type and update visual states
                _secondaryButton.PointerEnter       += Button_PointerEvent;
                _secondaryButton.PointerLeave       += Button_PointerEvent;
                _secondaryButton.PointerPressed     += Button_PointerEvent;
                _secondaryButton.PointerReleased    += Button_PointerEvent;
                _secondaryButton.PointerCaptureLost += Button_PointerCaptureLost;
            }

            RegisterFlyoutEvents(Flyout);
            UpdatePseudoClasses();

            _hasLoaded = true;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
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
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> e)
        {
            if (e.Property == CommandProperty)
            {
                if (_isAttachedToLogicalTree)
                {
                    // Must unregister events here while a reference to the old command still exists
                    if (e.OldValue.GetValueOrDefault() is ICommand oldCommand)
                    {
                        oldCommand.CanExecuteChanged -= CanExecuteChanged;
                    }

                    if (e.NewValue.GetValueOrDefault() is ICommand newCommand)
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
                // Must unregister events here while a reference to the old flyout still exists
                if (e.OldValue.GetValueOrDefault() is FlyoutBase oldFlyout)
                {
                    UnregisterFlyoutEvents(oldFlyout);
                }

                if (e.NewValue.GetValueOrDefault() is FlyoutBase newFlyout)
                {
                    RegisterFlyoutEvents(newFlyout);
                }

                UpdatePseudoClasses();
            }

            base.OnPropertyChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var key = e.Key;

            if (key == Key.Space || key == Key.Enter) // Key.GamepadA is not currently supported
            {
                _isKeyDown = true;
                UpdatePseudoClasses();
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            var key = e.Key;

            if (key == Key.Space || key == Key.Enter) // Key.GamepadA is not currently supported
            {
                _isKeyDown = false;
                UpdatePseudoClasses();

                // Consider this a click on the primary button
                if (IsEffectivelyEnabled)
                {
                    OnClickPrimary(null);
                    e.Handled = true;
                }
            }
            else if (key == Key.Down && IsEffectivelyEnabled)
            {
                // WinUI requires the VirtualKey.Menu (alt) + VirtualKey.Down to open the flyout
                // Avalonia will only require Key.Down which is better cross-platform

                OpenFlyout();
                e.Handled = true;
            }
            else if (key == Key.F4 && IsEffectivelyEnabled)
            {
                OpenFlyout();
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }

        /// <summary>
        /// Invokes the <see cref="Click"/> event when the primary button part is clicked.
        /// </summary>
        /// <param name="e">The event args from the internal Click event.</param>
        protected virtual void OnClickPrimary(RoutedEventArgs e)
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
        protected virtual void OnClickSecondary(RoutedEventArgs e)
        {
            // Note: It is not currently required to check enabled status; however, this is a failsafe
            if (IsEffectivelyEnabled)
            {
                OpenFlyout();
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Event Handling
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Event handler for when the internal primary button part is pressed.
        /// </summary>
        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            OnClickPrimary(e);
        }

        /// <summary>
        /// Event handler for when the internal secondary button part is pressed.
        /// </summary>
        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            OnClickSecondary(e);
        }

        /// <summary>
        /// Event handler for when pointer events occur in the primary or secondary buttons.
        /// </summary>
        private void Button_PointerEvent(object sender, PointerEventArgs e)
        {
            // Warning: Code must match with Button_PointerCaptureLost
            if (_lastPointerType != e.Pointer.Type)
            {
                _lastPointerType = e.Pointer.Type;
                UpdatePseudoClasses();
            }
        }

        /// <summary>
        /// Event handler for when the pointer capture is lost in the primary or secondary buttons.
        /// </summary>
        /// <remarks>
        /// In upstream WinUI this is not a separate event handler. However, Avalonia has different args.
        /// </remarks>
        private void Button_PointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
        {
            // Warning: Code must match with Button_PointerEvent
            if (_lastPointerType != e.Pointer.Type)
            {
                _lastPointerType = e.Pointer.Type;
                UpdatePseudoClasses();
            }
        }

        /// <summary>
        /// Called when a primary or secondary button property changes that affects the visual states.
        /// </summary>
        private void Button_VisualPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Called when the <see cref="FlyoutBase.Placement"/> property changes.
        /// </summary>
        private void Flyout_PlacementPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Event handler for when the split button's flyout is opened.
        /// </summary>
        private void Flyout_Opened(object sender, EventArgs e)
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
            }
        }

        /// <summary>
        /// Event handler for when the split button's flyout is closed.
        /// </summary>
        private void Flyout_Closed(object sender, EventArgs e)
        {
            var flyout = sender as FlyoutBase;

            // See comments in Flyout_Opened
            if (object.ReferenceEquals(flyout?.Target, this))
            {
                _isFlyoutOpen = false;
                UpdatePseudoClasses();
            }
        }
    }
}
