using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A button with primary and secondary parts that can each be invoked separately.
    /// The primary part behaves like a button and the secondary part opens a flyout.
    /// </summary>
    public class SplitButton : ContentControl, ICommandSource
    {
        /// <summary>
        /// Raised when the user presses the primary part of the <see cref="SplitButton"/>.
        /// </summary>
        public event EventHandler<SplitButtonClickEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<SplitButtonClickEventArgs> ClickEvent =
            RoutedEvent.Register<SplitButton, SplitButtonClickEventArgs>(
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

        private bool        _commandCanExecute = true;
        protected bool      _hasLoaded         = false;
        private bool        _isFlyoutOpen      = false;
        private bool        _isKeyDown         = false;
        private PointerType _lastPointerType   = PointerType.Mouse;

        private CompositeDisposable _buttonPropertyChangedDisposable;
        private IDisposable         _flyoutPropertyChangedDisposable;

        ////////////////////////////////////////////////////////////////////////
        // 
        // Constructor / Destructors
        //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitButton"/> class.
        /// </summary>
        public SplitButton()
        {
        }

        ////////////////////////////////////////////////////////////////////////
        // 
        // Properties
        //
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

        ////////////////////////////////////////////////////////////////////////
        // 
        // Methods
        //
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

        /// <inheritdoc/>
        protected override bool IsEnabledCore => base.IsEnabledCore && _commandCanExecute;

        /// <summary>
        /// Updates the visual state of the control by applying latest PseudoClasses.
        /// </summary>
        private void UpdatePseudoClasses()
        {
            /*
            // place the secondary button
            if (m_lastPointerDeviceType == winrt::PointerDeviceType::Touch || m_isKeyDown)
            {
                winrt::VisualStateManager::GoToState(*this, L"SecondaryButtonSpan", useTransitions);
            }
            else
            {
                winrt::VisualStateManager::GoToState(*this, L"SecondaryButtonRight", useTransitions);
            }

            // change visual state
            auto primaryButton = m_primaryButton.get();
            auto secondaryButton = m_secondaryButton.get();

            if (!IsEnabled())
            {
                winrt::VisualStateManager::GoToState(*this, L"Disabled", useTransitions);
            }
            else if (primaryButton && secondaryButton)
            {
                if (m_isFlyoutOpen)
                {
                    if (InternalIsChecked())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"CheckedFlyoutOpen", useTransitions);
                    }
                    else
                    {
                        winrt::VisualStateManager::GoToState(*this, L"FlyoutOpen", useTransitions);
                    }
                }
                // SplitButton and ToggleSplitButton share a template -- this section is driving the checked states for ToggleSplitButton.
                else if (InternalIsChecked())
                {
                    if (m_lastPointerDeviceType == winrt::PointerDeviceType::Touch || m_isKeyDown)
                    {
                        if (primaryButton.IsPressed() || secondaryButton.IsPressed() || m_isKeyDown)
                        {
                            winrt::VisualStateManager::GoToState(*this, L"CheckedTouchPressed", useTransitions);
                        }
                        else
                        {
                            winrt::VisualStateManager::GoToState(*this, L"Checked", useTransitions);
                        }
                    }
                    else if (primaryButton.IsPressed())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"CheckedPrimaryPressed", useTransitions);
                    }
                    else if (primaryButton.IsPointerOver())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"CheckedPrimaryPointerOver", useTransitions);
                    }
                    else if (secondaryButton.IsPressed())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"CheckedSecondaryPressed", useTransitions);
                    }
                    else if (secondaryButton.IsPointerOver())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"CheckedSecondaryPointerOver", useTransitions);
                    }
                    else
                    {
                        winrt::VisualStateManager::GoToState(*this, L"Checked", useTransitions);
                    }
                }
                else
                {
                    if (m_lastPointerDeviceType == winrt::PointerDeviceType::Touch || m_isKeyDown)
                    {
                        if (primaryButton.IsPressed() || secondaryButton.IsPressed() || m_isKeyDown)
                        {
                            winrt::VisualStateManager::GoToState(*this, L"TouchPressed", useTransitions);
                        }
                        else
                        {
                            winrt::VisualStateManager::GoToState(*this, L"Normal", useTransitions);
                        }
                    }
                    else if (primaryButton.IsPressed())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"PrimaryPressed", useTransitions);
                    }
                    else if (primaryButton.IsPointerOver())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"PrimaryPointerOver", useTransitions);
                    }
                    else if (secondaryButton.IsPressed())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"SecondaryPressed", useTransitions);
                    }
                    else if (secondaryButton.IsPointerOver())
                    {
                        winrt::VisualStateManager::GoToState(*this, L"SecondaryPointerOver", useTransitions);
                    }
                    else
                    {
                        winrt::VisualStateManager::GoToState(*this, L"Normal", useTransitions);
                    }
                }
            }
            */
        }

        /// <summary>
        /// Opens the secondary button's flyout.
        /// </summary>
        private void OpenFlyout()
        {
            if (Flyout != null)
            {
                Flyout.ShowAt(this);
            }
        }

        /// <summary>
        /// Closes the secondary button's flyout.
        /// </summary>
        private void CloseFlyout()
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
        // 
        // OnEvent Overridable Methods
        //
        ////////////////////////////////////////////////////////////////////////

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            UnregisterEvents();
            UnregisterFlyoutEvents(Flyout);

            _primaryButton   = e.NameScope.Find<Button>("PrimaryButton");
            _secondaryButton = e.NameScope.Find<Button>("SecondaryButton");

            _buttonPropertyChangedDisposable = new CompositeDisposable();

            if (_primaryButton != null)
            {
                _primaryButton.Click += PrimaryButton_Click;

                _primaryButton.GetPropertyChangedObservable(Button.IsPressedProperty);

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
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> changedEventArgs)
        {
            if (changedEventArgs.Property == FlyoutProperty)
            {
                // Must unregister events here while a ref to the old flyout still exists
                if (changedEventArgs.OldValue.GetValueOrDefault() is FlyoutBase oldFlyout)
                {
                    UnregisterFlyoutEvents(oldFlyout);
                }

                if (changedEventArgs.NewValue.GetValueOrDefault() is FlyoutBase newFlyout)
                {
                    RegisterFlyoutEvents(newFlyout);
                }

                UpdatePseudoClasses();
            }

            base.OnPropertyChanged(changedEventArgs);
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
        /// Invokes the <see cref="Click"/> when the primary button part is clicked.
        /// </summary>
        /// <param name="e">The event args from the internal Click event.</param>
        protected virtual void OnClickPrimary(RoutedEventArgs e)
        {
            // Note: It is not currently required to check enabled status; however, this is a failsafe
            if (IsEffectivelyEnabled)
            {
                var eventArgs = new SplitButtonClickEventArgs(ClickEvent);
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
        // 
        // Event Handling
        //
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
            _isFlyoutOpen = true;
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Event handler for when the split button's flyout is closed.
        /// </summary>
        private void Flyout_Closed(object sender, EventArgs e)
        {
            _isFlyoutOpen = false;
            UpdatePseudoClasses();
        }
    }
}
