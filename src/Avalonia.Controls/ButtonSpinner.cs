using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Reactive;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    public enum Location
    {
        Left,
        Right
    }

    /// <summary>
    /// Represents a spinner control that includes two Buttons.
    /// </summary>
    [TemplatePart("PART_DecreaseButton", typeof(Button))]
    [TemplatePart("PART_IncreaseButton", typeof(Button))]
    [PseudoClasses(":left", ":right")]
    public class ButtonSpinner : Spinner
    {
        /// <summary>
        /// Defines the <see cref="AllowSpin"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowSpinProperty =
            AvaloniaProperty.Register<ButtonSpinner, bool>(nameof(AllowSpin), true);

        /// <summary>
        /// Defines the <see cref="ShowButtonSpinner"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowButtonSpinnerProperty =
            AvaloniaProperty.Register<ButtonSpinner, bool>(nameof(ShowButtonSpinner), true);

        /// <summary>
        /// Defines the <see cref="ButtonSpinnerLocation"/> property.
        /// </summary>
        public static readonly StyledProperty<Location> ButtonSpinnerLocationProperty =
            AvaloniaProperty.Register<ButtonSpinner, Location>(nameof(ButtonSpinnerLocation), Location.Right);

        public ButtonSpinner()
        {
            UpdatePseudoClasses(ButtonSpinnerLocation);
        }

        private Button? _decreaseButton;
        /// <summary>
        /// Gets or sets the DecreaseButton template part.
        /// </summary>
        private Button? DecreaseButton
        {
            get => _decreaseButton;
            set
            {
                if (_decreaseButton != null)
                {
                    _decreaseButton.Click -= OnButtonClick;
                }
                _decreaseButton = value;
                if (_decreaseButton != null)
                {
                    _decreaseButton.Click += OnButtonClick;
                }
            }
        }

        private Button? _increaseButton;
        /// <summary>
        /// Gets or sets the IncreaseButton template part.
        /// </summary>
        private Button? IncreaseButton
        {
            get => _increaseButton;
            set
            {
                if (_increaseButton != null)
                {
                    _increaseButton.Click -= OnButtonClick;
                }
                _increaseButton = value;
                if (_increaseButton != null)
                {
                    _increaseButton.Click += OnButtonClick;
                }
            }
        }

        /// <summary>
        /// Initializes static members of the <see cref="ButtonSpinner"/> class.
        /// </summary>
        static ButtonSpinner()
        {
            AllowSpinProperty.Changed.Subscribe(AllowSpinChanged);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ButtonSpinner"/> should allow to spin.
        /// </summary>
        public bool AllowSpin
        {
            get => GetValue(AllowSpinProperty);
            set => SetValue(AllowSpinProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the spin buttons should be shown.
        /// </summary>
        public bool ShowButtonSpinner
        {
            get => GetValue(ShowButtonSpinnerProperty);
            set => SetValue(ShowButtonSpinnerProperty, value);
        }

        /// <summary>
        /// Gets or sets current location of the <see cref="ButtonSpinner"/>.
        /// </summary>
        public Location ButtonSpinnerLocation
        {
            get => GetValue(ButtonSpinnerLocationProperty);
            set => SetValue(ButtonSpinnerLocationProperty, value);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            IncreaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");
            DecreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
            SetButtonUsage();
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            Point mousePosition;
            if (IncreaseButton != null && IncreaseButton.IsEnabled == false)
            {
                mousePosition = e.GetPosition(IncreaseButton);
                if (mousePosition.X > 0 && mousePosition.X < IncreaseButton.Width &&
                    mousePosition.Y > 0 && mousePosition.Y < IncreaseButton.Height)
                {
                    e.Handled = true;
                }
            }

            if (DecreaseButton != null && DecreaseButton.IsEnabled == false)
            {
                mousePosition = e.GetPosition(DecreaseButton);
                if (mousePosition.X > 0 && mousePosition.X < DecreaseButton.Width &&
                    mousePosition.Y > 0 && mousePosition.Y < DecreaseButton.Height)
                {
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // If XY navigation is enabled - do not spin with arrow keys, instead use spinner buttons.
            if (this.IsAllowedXYNavigationMode(e.KeyDeviceType))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Up:
                {
                    if (AllowSpin)
                    {
                        OnSpin(new SpinEventArgs(SpinEvent, SpinDirection.Increase));
                        e.Handled = true;
                    }
                    break;
                }
                case Key.Down:
                {
                    if (AllowSpin)
                    {
                        OnSpin(new SpinEventArgs(SpinEvent, SpinDirection.Decrease));
                        e.Handled = true;
                    }
                    break;
                }
                case Key.Enter:
                {
                    //Do not Spin on enter Key when spinners have focus
                    if (((IncreaseButton != null) && (IncreaseButton.IsFocused))
                        || ((DecreaseButton != null) && DecreaseButton.IsFocused))
                    {
                        e.Handled = true;
                    }
                    break;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (AllowSpin && IsKeyboardFocusWithin)
            {
                if (e.Delta.Y != 0)
                {
                    var spinnerEventArgs = new SpinEventArgs(SpinEvent, (e.Delta.Y < 0) ? SpinDirection.Decrease : SpinDirection.Increase, true);
                    OnSpin(spinnerEventArgs);
                    e.Handled = true;
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ButtonSpinnerLocationProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<Location>());
            }
        }

        protected override void OnValidSpinDirectionChanged(ValidSpinDirections oldValue, ValidSpinDirections newValue)
        {
            SetButtonUsage();
        }

        /// <summary>
        /// Called when the <see cref="AllowSpin"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnAllowSpinChanged(bool oldValue, bool newValue)
        {
            SetButtonUsage();
        }

        /// <summary>
        /// Called when the <see cref="AllowSpin"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AllowSpinChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is ButtonSpinner spinner)
            {
                var oldValue = (bool)e.OldValue!;
                var newValue = (bool)e.NewValue!;
                spinner.OnAllowSpinChanged(oldValue, newValue);
            }
        }
        
        /// <summary>
        /// Disables or enables the buttons based on the valid spin direction.
        /// </summary>
        private void SetButtonUsage()
        {
            if (IncreaseButton != null)
            {
                IncreaseButton.IsEnabled = AllowSpin && ((ValidSpinDirection & ValidSpinDirections.Increase) == ValidSpinDirections.Increase);
            }

            if (DecreaseButton != null)
            {
                DecreaseButton.IsEnabled = AllowSpin && ((ValidSpinDirection & ValidSpinDirections.Decrease) == ValidSpinDirections.Decrease);
            }
        }

        /// <summary>
        /// Called when user clicks one of the spin buttons.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            if (AllowSpin)
            {
                var direction = sender == IncreaseButton ? SpinDirection.Increase : SpinDirection.Decrease;
                OnSpin(new SpinEventArgs(SpinEvent, direction));
            }
        }

        private void UpdatePseudoClasses(Location location)
        {
            PseudoClasses.Set(":left", location == Location.Left);
            PseudoClasses.Set(":right", location == Location.Right);
        }
    }
}
