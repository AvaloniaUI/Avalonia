using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Control that represents a TextBox with button spinners that allow incrementing and decrementing numeric values.
    /// </summary>
    public class NumericUpDown : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="AllowSpin"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowSpinProperty =
            ButtonSpinner.AllowSpinProperty.AddOwner<NumericUpDown>();

        /// <summary>
        /// Defines the <see cref="ButtonSpinnerLocation"/> property.
        /// </summary>
        public static readonly StyledProperty<Location> ButtonSpinnerLocationProperty =
            ButtonSpinner.ButtonSpinnerLocationProperty.AddOwner<NumericUpDown>();

        /// <summary>
        /// Defines the <see cref="ShowButtonSpinner"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowButtonSpinnerProperty =
            ButtonSpinner.ShowButtonSpinnerProperty.AddOwner<NumericUpDown>();

        /// <summary>
        /// Defines the <see cref="ClipValueToMinMax"/> property.
        /// </summary>
        public static readonly DirectProperty<NumericUpDown, bool> ClipValueToMinMaxProperty =
            AvaloniaProperty.RegisterDirect<NumericUpDown, bool>(nameof(ClipValueToMinMax),
                updown => updown.ClipValueToMinMax, (updown, b) => updown.ClipValueToMinMax = b);

        /// <summary>
        /// Defines the <see cref="CultureInfo"/> property.
        /// </summary>
        public static readonly DirectProperty<NumericUpDown, CultureInfo> CultureInfoProperty =
            AvaloniaProperty.RegisterDirect<NumericUpDown, CultureInfo>(nameof(CultureInfo), o => o.CultureInfo,
                (o, v) => o.CultureInfo = v, CultureInfo.CurrentCulture);

        /// <summary>
        /// Defines the <see cref="FormatString"/> property.
        /// </summary>
        public static readonly StyledProperty<string> FormatStringProperty =
            AvaloniaProperty.Register<NumericUpDown, string>(nameof(FormatString), string.Empty);

        /// <summary>
        /// Defines the <see cref="Increment"/> property.
        /// </summary>
        public static readonly StyledProperty<double> IncrementProperty =
            AvaloniaProperty.Register<NumericUpDown, double>(nameof(Increment), 1.0d, coerce: OnCoerceIncrement);

        /// <summary>
        /// Defines the <see cref="IsReadOnly"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<NumericUpDown, bool>(nameof(IsReadOnly));

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<NumericUpDown, double>(nameof(Maximum), double.MaxValue, coerce: OnCoerceMaximum);

        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<NumericUpDown, double>(nameof(Minimum), double.MinValue, coerce: OnCoerceMinimum);

        /// <summary>
        /// Defines the <see cref="ParsingNumberStyle"/> property.
        /// </summary>
        public static readonly DirectProperty<NumericUpDown, NumberStyles> ParsingNumberStyleProperty =
            AvaloniaProperty.RegisterDirect<NumericUpDown, NumberStyles>(nameof(ParsingNumberStyle),
                updown => updown.ParsingNumberStyle, (updown, style) => updown.ParsingNumberStyle = style);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<NumericUpDown, string> TextProperty =
            AvaloniaProperty.RegisterDirect<NumericUpDown, string>(nameof(Text), o => o.Text, (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly DirectProperty<NumericUpDown, double> ValueProperty =
            AvaloniaProperty.RegisterDirect<NumericUpDown, double>(nameof(Value), updown => updown.Value,
                (updown, v) => updown.Value = v, defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="Watermark"/> property.
        /// </summary>
        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<NumericUpDown, string>(nameof(Watermark));

        private IDisposable _textBoxTextChangedSubscription;

        private double _value;
        private string _text;
        private bool _internalValueSet;
        private bool _clipValueToMinMax;
        private bool _isSyncingTextAndValueProperties;
        private bool _isTextChangedFromUI;
        private CultureInfo _cultureInfo;
        private NumberStyles _parsingNumberStyle = NumberStyles.Any;
        
        /// <summary>
        /// Gets the Spinner template part.
        /// </summary>
        private Spinner Spinner { get; set; }

        /// <summary>
        /// Gets the TextBox template part.
        /// </summary>
        private TextBox TextBox { get; set; }

        /// <summary>
        /// Gets or sets the ability to perform increment/decrement operations via the keyboard, button spinners, or mouse wheel.
        /// </summary>
        public bool AllowSpin
        {
            get { return GetValue(AllowSpinProperty); }
            set { SetValue(AllowSpinProperty, value); }
        }

        /// <summary>
        /// Gets or sets current location of the <see cref="ButtonSpinner"/>.
        /// </summary>
        public Location ButtonSpinnerLocation
        {
            get { return GetValue(ButtonSpinnerLocationProperty); }
            set { SetValue(ButtonSpinnerLocationProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the spin buttons should be shown.
        /// </summary>
        public bool ShowButtonSpinner
        {
            get { return GetValue(ShowButtonSpinnerProperty); }
            set { SetValue(ShowButtonSpinnerProperty, value); }
        }

        /// <summary>
        /// Gets or sets if the value should be clipped when minimum/maximum is reached.
        /// </summary>
        public bool ClipValueToMinMax
        {
            get { return _clipValueToMinMax; }
            set { SetAndRaise(ClipValueToMinMaxProperty, ref _clipValueToMinMax, value); }
        }

        /// <summary>
        /// Gets or sets the current CultureInfo.
        /// </summary>
        public CultureInfo CultureInfo
        {
            get { return _cultureInfo; }
            set { SetAndRaise(CultureInfoProperty, ref _cultureInfo, value); }
        }

        /// <summary>
        /// Gets or sets the display format of the <see cref="Value"/>.
        /// </summary>
        public string FormatString
        {
            get { return GetValue(FormatStringProperty); }
            set { SetValue(FormatStringProperty, value); }
        }

        /// <summary>
        /// Gets or sets the amount in which to increment the <see cref="Value"/>.
        /// </summary>
        public double Increment
        {
            get { return GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        /// <summary>
        /// Gets or sets if the control is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum allowed value.
        /// </summary>
        public double Maximum
        {
            get { return GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum allowed value.
        /// </summary>
        public double Minimum
        {
            get { return GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the parsing style (AllowLeadingWhite, Float, AllowHexSpecifier, ...). By default, Any.
        /// </summary>
        public NumberStyles ParsingNumberStyle
        {
            get { return _parsingNumberStyle; }
            set { SetAndRaise(ParsingNumberStyleProperty, ref _parsingNumberStyle, value); }
        }

        /// <summary>
        /// Gets or sets the formatted string representation of the value.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value
        {
            get { return _value; }
            set
            {
                value = OnCoerceValue(value);
                SetAndRaise(ValueProperty, ref _value, value);
            }
        }

        /// <summary>
        /// Gets or sets the object to use as a watermark if the <see cref="Value"/> is null.
        /// </summary>
        public string Watermark
        {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        /// <summary>
        /// Initializes new instance of <see cref="NumericUpDown"/> class.
        /// </summary>
        public NumericUpDown()
        {
            Initialized += (sender, e) =>
            {
                if (!_internalValueSet && IsInitialized)
                {
                    SyncTextAndValueProperties(false, null, true);
                }

                SetValidSpinDirection();
            };
        }

        /// <summary>
        /// Initializes static members of the <see cref="NumericUpDown"/> class.
        /// </summary>
        static NumericUpDown()
        {
            CultureInfoProperty.Changed.Subscribe(OnCultureInfoChanged);
            FormatStringProperty.Changed.Subscribe(FormatStringChanged);
            IncrementProperty.Changed.Subscribe(IncrementChanged);
            IsReadOnlyProperty.Changed.Subscribe(OnIsReadOnlyChanged);
            MaximumProperty.Changed.Subscribe(OnMaximumChanged);
            MinimumProperty.Changed.Subscribe(OnMinimumChanged);
            TextProperty.Changed.Subscribe(OnTextChanged);
            ValueProperty.Changed.Subscribe(OnValueChanged);
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            CommitInput();
            base.OnLostFocus(e);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (TextBox != null)
            {
                TextBox.PointerPressed -= TextBoxOnPointerPressed;
                _textBoxTextChangedSubscription?.Dispose();
            }
            TextBox = e.NameScope.Find<TextBox>("PART_TextBox");
            if (TextBox != null)
            {
                TextBox.Text = Text;
                TextBox.PointerPressed += TextBoxOnPointerPressed;
                _textBoxTextChangedSubscription = TextBox.GetObservable(TextBox.TextProperty).Subscribe(txt => TextBoxOnTextChanged());
            }

            if (Spinner != null)
            {
                Spinner.Spin -= OnSpinnerSpin;
            }

            Spinner = e.NameScope.Find<Spinner>("PART_Spinner");

            if (Spinner != null)
            {
                Spinner.Spin += OnSpinnerSpin;
            }

            SetValidSpinDirection();
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    var commitSuccess = CommitInput();
                    e.Handled = !commitSuccess;
                    break;
            }
        }

        /// <summary>
        /// Called when the <see cref="CultureInfo"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnCultureInfoChanged(CultureInfo oldValue, CultureInfo newValue)
        {
            if (IsInitialized)
            {
                SyncTextAndValueProperties(false, null);
            }
        }

        /// <summary>
        /// Called when the <see cref="FormatString"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnFormatStringChanged(string oldValue, string newValue)
        {
            if (IsInitialized)
            {
                SyncTextAndValueProperties(false, null);
            }
        }

        /// <summary>
        /// Called when the <see cref="Increment"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnIncrementChanged(double oldValue, double newValue)
        {
            if (IsInitialized)
            {
                SetValidSpinDirection();
            }
        }

        /// <summary>
        /// Called when the <see cref="IsReadOnly"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
            SetValidSpinDirection();
        }

        /// <summary>
        /// Called when the <see cref="Maximum"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnMaximumChanged(double oldValue, double newValue)
        {
            if (IsInitialized)
            {
                SetValidSpinDirection();
            }
            if (ClipValueToMinMax)
            {
                Value = MathUtilities.Clamp(Value, Minimum, Maximum);
            }
        }

        /// <summary>
        /// Called when the <see cref="Minimum"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnMinimumChanged(double oldValue, double newValue)
        {
            if (IsInitialized)
            {
                SetValidSpinDirection();
            }
            if (ClipValueToMinMax)
            {
                Value = MathUtilities.Clamp(Value, Minimum, Maximum);
            }
        }

        /// <summary>
        /// Called when the <see cref="Text"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
            if (IsInitialized)
            {
                SyncTextAndValueProperties(true, Text);
            }
        }

        /// <summary>
        /// Called when the <see cref="Value"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
            if (!_internalValueSet && IsInitialized)
            {
                SyncTextAndValueProperties(false, null, true);
            }

            SetValidSpinDirection();

            RaiseValueChangedEvent(oldValue, newValue);
        }

        /// <summary>
        /// Called when the <see cref="Increment"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual double OnCoerceIncrement(double baseValue)
        {
            return baseValue;
        }

        /// <summary>
        /// Called when the <see cref="Maximum"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual double OnCoerceMaximum(double baseValue)
        {
            return Math.Max(baseValue, Minimum);
        }

        /// <summary>
        /// Called when the <see cref="Minimum"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual double OnCoerceMinimum(double baseValue)
        {
            return Math.Min(baseValue, Maximum);
        }

        /// <summary>
        /// Called when the <see cref="Value"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual double OnCoerceValue(double baseValue)
        {
            return baseValue;
        }

        /// <summary>
        /// Raises the OnSpin event when spinning is initiated by the end-user.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnSpin(SpinEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var handler = Spinned;
            handler?.Invoke(this, e);

            if (e.Direction == SpinDirection.Increase)
            {
                DoIncrement();
            }
            else
            {
                DoDecrement();
            }
        }

        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void RaiseValueChangedEvent(double oldValue, double newValue)
        {
            var e = new NumericUpDownValueChangedEventArgs(ValueChangedEvent, oldValue, newValue);
            RaiseEvent(e);
        }

        /// <summary>
        /// Converts the formatted text to a value.
        /// </summary>
        private double ConvertTextToValue(string text)
        {
            double result = 0;

            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            // Since the conversion from Value to text using a FormatString may not be parsable,
            // we verify that the already existing text is not the exact same value.
            var currentValueText = ConvertValueToText();
            if (Equals(currentValueText, text))
            {
                return Value;
            }

            result = ConvertTextToValueCore(currentValueText, text);

            if (ClipValueToMinMax)
            {
                return MathUtilities.Clamp(result, Minimum, Maximum);
            }

            ValidateMinMax(result);

            return result;
        }

        /// <summary>
        /// Converts the value to formatted text.
        /// </summary>
        /// <returns></returns>
        private string ConvertValueToText()
        {
            //Manage FormatString of type "{}{0:N2} °" (in xaml) or "{0:N2} °" in code-behind.
            if (FormatString.Contains("{0"))
            {
                return string.Format(CultureInfo, FormatString, Value);
            }

            return Value.ToString(FormatString, CultureInfo);
        }

        /// <summary>
        /// Called by OnSpin when the spin direction is SpinDirection.Increase.
        /// </summary>
        private void OnIncrement()
        {
            var result = Value + Increment;
            Value = MathUtilities.Clamp(result, Minimum, Maximum);
        }

        /// <summary>
        /// Called by OnSpin when the spin direction is SpinDirection.Decrease.
        /// </summary>
        private void OnDecrement()
        {
            var result = Value - Increment;
            Value = MathUtilities.Clamp(result, Minimum, Maximum);
        }

        /// <summary>
        /// Sets the valid spin directions.
        /// </summary>
        private void SetValidSpinDirection()
        {
            var validDirections = ValidSpinDirections.None;

            // Zero increment always prevents spin.
            if (Increment != 0 && !IsReadOnly)
            {
                if (Value < Maximum)
                {
                    validDirections = validDirections | ValidSpinDirections.Increase;
                }

                if (Value > Minimum)
                {
                    validDirections = validDirections | ValidSpinDirections.Decrease;
                }
            }

            if (Spinner != null)
            {
                Spinner.ValidSpinDirection = validDirections;
            }
        }

        /// <summary>
        /// Called when the <see cref="CultureInfo"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnCultureInfoChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (CultureInfo)e.OldValue;
                var newValue = (CultureInfo)e.NewValue;
                upDown.OnCultureInfoChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Increment"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void IncrementChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (double)e.OldValue;
                var newValue = (double)e.NewValue;
                upDown.OnIncrementChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="FormatString"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void FormatStringChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (string)e.OldValue;
                var newValue = (string)e.NewValue;
                upDown.OnFormatStringChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="IsReadOnly"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnIsReadOnlyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (bool)e.OldValue;
                var newValue = (bool)e.NewValue;
                upDown.OnIsReadOnlyChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Maximum"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnMaximumChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (double)e.OldValue;
                var newValue = (double)e.NewValue;
                upDown.OnMaximumChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Minimum"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnMinimumChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (double)e.OldValue;
                var newValue = (double)e.NewValue;
                upDown.OnMinimumChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Text"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnTextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (string)e.OldValue;
                var newValue = (string)e.NewValue;
                upDown.OnTextChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Value"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnValueChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown upDown)
            {
                var oldValue = (double)e.OldValue;
                var newValue = (double)e.NewValue;
                upDown.OnValueChanged(oldValue, newValue);
            }
        }

        private void SetValueInternal(double value)
        {
            _internalValueSet = true;
            try
            {
                Value = value;
            }
            finally
            {
                _internalValueSet = false;
            }
        }

        private static double OnCoerceMaximum(IAvaloniaObject instance, double value)
        {
            if (instance is NumericUpDown upDown)
            {
                return upDown.OnCoerceMaximum(value);
            }

            return value;
        }

        private static double OnCoerceMinimum(IAvaloniaObject instance, double value)
        {
            if (instance is NumericUpDown upDown)
            {
                return upDown.OnCoerceMinimum(value);
            }

            return value;
        }

        private static double OnCoerceIncrement(IAvaloniaObject instance, double value)
        {
            if (instance is NumericUpDown upDown)
            {
                return upDown.OnCoerceIncrement(value);
            }

            return value;
        }

        private void TextBoxOnTextChanged()
        {
            try
            {
                _isTextChangedFromUI = true;
                if (TextBox != null)
                {
                    Text = TextBox.Text;
                }
            }
            finally
            {
                _isTextChangedFromUI = false;
            }
        }

        private void OnSpinnerSpin(object sender, SpinEventArgs e)
        {
            if (AllowSpin && !IsReadOnly)
            {
                var spin = !e.UsingMouseWheel;
                spin |= ((TextBox != null) && TextBox.IsFocused);

                if (spin)
                {
                    e.Handled = true;
                    OnSpin(e);
                }
            }
        }

        private void DoDecrement()
        {
            if (Spinner == null || (Spinner.ValidSpinDirection & ValidSpinDirections.Decrease) == ValidSpinDirections.Decrease)
            {
                OnDecrement();
            }
        }

        private void DoIncrement()
        {
            if (Spinner == null || (Spinner.ValidSpinDirection & ValidSpinDirections.Increase) == ValidSpinDirections.Increase)
            {
                OnIncrement();
            }
        }

        public event EventHandler<SpinEventArgs> Spinned;

        private void TextBoxOnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.Pointer.Captured != Spinner)
            {
                Dispatcher.UIThread.InvokeAsync(() => { e.Pointer.Capture(Spinner); }, DispatcherPriority.Input);
            }
        }

        /// <summary>
        /// Defines the <see cref="ValueChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<NumericUpDownValueChangedEventArgs> ValueChangedEvent =
            RoutedEvent.Register<NumericUpDown, NumericUpDownValueChangedEventArgs>(nameof(ValueChanged), RoutingStrategies.Bubble);

        /// <summary>
        /// Raised when the <see cref="Value"/> changes.
        /// </summary>
        public event EventHandler<NumericUpDownValueChangedEventArgs> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private bool CommitInput()
        {
            return SyncTextAndValueProperties(true, Text);
        }

        /// <summary>
        /// Synchronize <see cref="Text"/> and <see cref="Value"/> properties.
        /// </summary>
        /// <param name="updateValueFromText">If value should be updated from text.</param>
        /// <param name="text">The text.</param>
        private bool SyncTextAndValueProperties(bool updateValueFromText, string text)
        {
            return SyncTextAndValueProperties(updateValueFromText, text, false);
        }

        /// <summary>
        /// Synchronize <see cref="Text"/> and <see cref="Value"/> properties.
        /// </summary>
        /// <param name="updateValueFromText">If value should be updated from text.</param>
        /// <param name="text">The text.</param>
        /// <param name="forceTextUpdate">Force text update.</param>
        private bool SyncTextAndValueProperties(bool updateValueFromText, string text, bool forceTextUpdate)
        {
            if (_isSyncingTextAndValueProperties)
                return true;

            _isSyncingTextAndValueProperties = true;
            var parsedTextIsValid = true;
            try
            {
                if (updateValueFromText)
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        try
                        {
                            var newValue = ConvertTextToValue(text);
                            if (!Equals(newValue, Value))
                            {
                                SetValueInternal(newValue);
                            }
                        }
                        catch
                        {
                            parsedTextIsValid = false;
                        }
                    }
                }

                // Do not touch the ongoing text input from user.
                if (!_isTextChangedFromUI)
                {
                    var keepEmpty = !forceTextUpdate && string.IsNullOrEmpty(Text);
                    if (!keepEmpty)
                    {
                        var newText = ConvertValueToText();
                        if (!Equals(Text, newText))
                        {
                            Text = newText;
                        }
                    }

                    // Sync Text and textBox
                    if (TextBox != null)
                    {
                        TextBox.Text = Text;
                    }
                }

                if (_isTextChangedFromUI && !parsedTextIsValid)
                {
                    // Text input was made from the user and the text
                    // represents an invalid value. Disable the spinner in this case.
                    if (Spinner != null)
                    {
                        Spinner.ValidSpinDirection = ValidSpinDirections.None;
                    }
                }
                else
                {
                    SetValidSpinDirection();
                }
            }
            finally
            {
                _isSyncingTextAndValueProperties = false;
            }
            return parsedTextIsValid;
        }

        private double ConvertTextToValueCore(string currentValueText, string text)
        {
            double result;

            if (IsPercent(FormatString))
            {
                result = decimal.ToDouble(ParsePercent(text, CultureInfo));
            }
            else
            {
                // Problem while converting new text
                if (!double.TryParse(text, ParsingNumberStyle, CultureInfo, out var outputValue))
                {
                    var shouldThrow = true;

                    // Check if CurrentValueText is also failing => it also contains special characters. ex : 90°
                    if (!double.TryParse(currentValueText, ParsingNumberStyle, CultureInfo, out var _))
                    {
                        // extract non-digit characters
                        var currentValueTextSpecialCharacters = currentValueText.Where(c => !char.IsDigit(c));
                        var textSpecialCharacters = text.Where(c => !char.IsDigit(c));
                        // same non-digit characters on currentValueText and new text => remove them on new Text to parse it again.
                        if (currentValueTextSpecialCharacters.Except(textSpecialCharacters).ToList().Count == 0)
                        {
                            foreach (var character in textSpecialCharacters)
                            {
                                text = text.Replace(character.ToString(), string.Empty);
                            }
                            // if without the special characters, parsing is good, do not throw
                            if (double.TryParse(text, ParsingNumberStyle, CultureInfo, out outputValue))
                            {
                                shouldThrow = false;
                            }
                        }
                    }

                    if (shouldThrow)
                    {
                        throw new InvalidDataException("Input string was not in a correct format.");
                    }
                }
                result = outputValue;
            }
            return result;
        }

        private void ValidateMinMax(double value)
        {
            if (value < Minimum)
            {
                throw new ArgumentOutOfRangeException(nameof(value), string.Format("Value must be greater than Minimum value of {0}", Minimum));
            }
            else if (value > Maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(value), string.Format("Value must be less than Maximum value of {0}", Maximum));
            }
        }

        /// <summary>
        /// Parse percent format text
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="cultureInfo">The culture info.</param>
        private static decimal ParsePercent(string text, IFormatProvider cultureInfo)
        {
            var info = NumberFormatInfo.GetInstance(cultureInfo);
            text = text.Replace(info.PercentSymbol, null);
            var result = decimal.Parse(text, NumberStyles.Any, info);
            result = result / 100;
            return result;
        }


        private bool IsPercent(string stringToTest)
        {
            var PIndex = stringToTest.IndexOf("P", StringComparison.Ordinal);
            if (PIndex >= 0)
            {
                //stringToTest contains a "P" between 2 "'", it's considered as text, not percent
                var isText = stringToTest.Substring(0, PIndex).Contains("'")
                             && stringToTest.Substring(PIndex, FormatString.Length - PIndex).Contains("'");

                return !isText;
            }
            return false;
        }
    }
}
