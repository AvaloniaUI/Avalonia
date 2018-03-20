using System;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    public abstract class UpDownBase : TemplatedControl
    {
    }

    /// <summary>
    /// Base class for controls that represents a TextBox with button spinners that allow incrementing and decrementing values.
    /// </summary>
    public abstract class UpDownBase<T> : UpDownBase
    {
        /// <summary>
        /// Defines the <see cref="AllowSpin"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowSpinProperty =
            ButtonSpinner.AllowSpinProperty.AddOwner<UpDownBase<T>>();

        /// <summary>
        /// Defines the <see cref="ButtonSpinnerLocation"/> property.
        /// </summary>
        public static readonly StyledProperty<Location> ButtonSpinnerLocationProperty =
            ButtonSpinner.ButtonSpinnerLocationProperty.AddOwner<UpDownBase<T>>();

        /// <summary>
        /// Defines the <see cref="ShowButtonSpinner"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowButtonSpinnerProperty =
            ButtonSpinner.ShowButtonSpinnerProperty.AddOwner<UpDownBase<T>>();

        /// <summary>
        /// Defines the <see cref="ClipValueToMinMax"/> property.
        /// </summary>
        public static readonly DirectProperty<UpDownBase<T>, bool> ClipValueToMinMaxProperty =
            AvaloniaProperty.RegisterDirect<UpDownBase<T>, bool>(nameof(ClipValueToMinMax),
                updown => updown.ClipValueToMinMax, (updown, b) => updown.ClipValueToMinMax = b);

        /// <summary>
        /// Defines the <see cref="CultureInfo"/> property.
        /// </summary>
        public static readonly DirectProperty<UpDownBase<T>, CultureInfo> CultureInfoProperty =
            AvaloniaProperty.RegisterDirect<UpDownBase<T>, CultureInfo>(nameof(CultureInfo), o => o.CultureInfo,
                (o, v) => o.CultureInfo = v, CultureInfo.CurrentCulture);

        /// <summary>
        /// Defines the <see cref="DefaultValue"/> property.
        /// </summary>
        public static readonly StyledProperty<T> DefaultValueProperty =
            AvaloniaProperty.Register<UpDownBase<T>, T>(nameof(DefaultValue));

        /// <summary>
        /// Defines the <see cref="DisplayDefaultValueOnEmptyText"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> DisplayDefaultValueOnEmptyTextProperty =
            AvaloniaProperty.Register<UpDownBase<T>, bool>(nameof(DisplayDefaultValueOnEmptyText));

        /// <summary>
        /// Defines the <see cref="IsReadOnly"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<UpDownBase<T>, bool>(nameof(IsReadOnly));

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly StyledProperty<T> MaximumProperty =
            AvaloniaProperty.Register<UpDownBase<T>, T>(nameof(Maximum), validate: OnCoerceMaximum);

        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly StyledProperty<T> MinimumProperty =
            AvaloniaProperty.Register<UpDownBase<T>, T>(nameof(Minimum), validate: OnCoerceMinimum);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<UpDownBase<T>, string> TextProperty =
            AvaloniaProperty.RegisterDirect<UpDownBase<T>, string>(nameof(Text), o => o.Text, (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly DirectProperty<UpDownBase<T>, T> ValueProperty =
            AvaloniaProperty.RegisterDirect<UpDownBase<T>, T>(nameof(Value), updown => updown.Value,
                (updown, v) => updown.Value = v, defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="Watermark"/> property.
        /// </summary>
        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<UpDownBase<T>, string>(nameof(Watermark));

        private IDisposable _textBoxTextChangedSubscription;
        private T _value;
        private string _text;
        private bool _internalValueSet;
        private bool _clipValueToMinMax;
        private bool _isSyncingTextAndValueProperties;
        private bool _isTextChangedFromUI;
        private CultureInfo _cultureInfo;

        /// <summary>
        /// Gets the Spinner template part.
        /// </summary>
        protected Spinner Spinner { get; private set; }

        /// <summary>
        /// Gets the TextBox template part.
        /// </summary>
        protected TextBox TextBox { get; private set; }

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
        /// Gets or sets the value to use when the <see cref="Value"/> is null and an increment/decrement operation is performed.
        /// </summary>
        public T DefaultValue
        {
            get { return GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets if the defaultValue should be displayed when the Text is empty.
        /// </summary>
        public bool DisplayDefaultValueOnEmptyText
        {
            get { return GetValue(DisplayDefaultValueOnEmptyTextProperty); }
            set { SetValue(DisplayDefaultValueOnEmptyTextProperty, value); }
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
        public T Maximum
        {
            get { return GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum allowed value.
        /// </summary>
        public T Minimum
        {
            get { return GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
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
        public T Value
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
        /// Initializes new instance of <see cref="UpDownBase{T}"/> class.
        /// </summary>
        protected UpDownBase()
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
        /// Initializes static members of the <see cref="UpDownBase{T}"/> class.
        /// </summary>
        static UpDownBase()
        {
            CultureInfoProperty.Changed.Subscribe(OnCultureInfoChanged);
            DefaultValueProperty.Changed.Subscribe(OnDefaultValueChanged);
            DisplayDefaultValueOnEmptyTextProperty.Changed.Subscribe(OnDisplayDefaultValueOnEmptyTextChanged);
            IsReadOnlyProperty.Changed.Subscribe(OnIsReadOnlyChanged);
            MaximumProperty.Changed.Subscribe(OnMaximumChanged);
            MinimumProperty.Changed.Subscribe(OnMinimumChanged);
            TextProperty.Changed.Subscribe(OnTextChanged);
            ValueProperty.Changed.Subscribe(OnValueChanged);
        }

        /// <inheritdoc />
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
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
        /// Called when the <see cref="DefaultValue"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnDefaultValueChanged(T oldValue, T newValue)
        {
            if (IsInitialized && string.IsNullOrEmpty(Text))
            {
                SyncTextAndValueProperties(true, Text);
            }
        }

        /// <summary>
        /// Called when the <see cref="DisplayDefaultValueOnEmptyText"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnDisplayDefaultValueOnEmptyTextChanged(bool oldValue, bool newValue)
        {
            if (IsInitialized && string.IsNullOrEmpty(Text))
            {
                SyncTextAndValueProperties(false, Text);
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
        protected virtual void OnMaximumChanged(T oldValue, T newValue)
        {
            if (IsInitialized)
            {
                SetValidSpinDirection();
            }
        }

        /// <summary>
        /// Called when the <see cref="Minimum"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnMinimumChanged(T oldValue, T newValue)
        {
            if (IsInitialized)
            {
                SetValidSpinDirection();
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
        protected virtual void OnValueChanged(T oldValue, T newValue)
        {
            if (!_internalValueSet && IsInitialized)
            {
                SyncTextAndValueProperties(false, null, true);
            }

            SetValidSpinDirection();

            RaiseValueChangedEvent(oldValue, newValue);
        }

        /// <summary>
        /// Called when the <see cref="Maximum"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual T OnCoerceMaximum(T baseValue)
        {
            return baseValue;
        }

        /// <summary>
        /// Called when the <see cref="Minimum"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual T OnCoerceMinimum(T baseValue)
        {
            return baseValue;
        }

        /// <summary>
        /// Called when the <see cref="Value"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual T OnCoerceValue(T baseValue)
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
                throw new ArgumentNullException("e");
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
        protected virtual void RaiseValueChangedEvent(T oldValue, T newValue)
        {
            var e = new UpDownValueChangedEventArgs<T>(ValueChangedEvent, oldValue, newValue);
            RaiseEvent(e);
        }

        /// <summary>
        /// Converts the formatted text to a value.
        /// </summary>
        protected abstract T ConvertTextToValue(string text);

        /// <summary>
        /// Converts the value to formatted text.
        /// </summary>
        /// <returns></returns>
        protected abstract string ConvertValueToText();

        /// <summary>
        /// Called by OnSpin when the spin direction is SpinDirection.Increase.
        /// </summary>
        protected abstract void OnIncrement();

        /// <summary>
        /// Called by OnSpin when the spin direction is SpinDirection.Descrease.
        /// </summary>
        protected abstract void OnDecrement();

        /// <summary>
        /// Sets the valid spin directions.
        /// </summary>
        protected abstract void SetValidSpinDirection();

        /// <summary>
        /// Called when the <see cref="CultureInfo"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnCultureInfoChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is UpDownBase<T> upDown)
            {
                var oldValue = (CultureInfo)e.OldValue;
                var newValue = (CultureInfo)e.NewValue;
                upDown.OnCultureInfoChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="DefaultValue"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnDefaultValueChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is UpDownBase<T> upDown)
            {
                var oldValue = (T)e.OldValue;
                var newValue = (T)e.NewValue;
                upDown.OnDefaultValueChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="DisplayDefaultValueOnEmptyText"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnDisplayDefaultValueOnEmptyTextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is UpDownBase<T> upDown)
            {
                var oldValue = (bool) e.OldValue;
                var newValue = (bool) e.NewValue;
                upDown.OnDisplayDefaultValueOnEmptyTextChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="IsReadOnly"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnIsReadOnlyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is UpDownBase<T> upDown)
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
            if (e.Sender is UpDownBase<T> upDown)
            {
                var oldValue = (T)e.OldValue;
                var newValue = (T)e.NewValue;
                upDown.OnMaximumChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Minimum"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnMinimumChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is UpDownBase<T> upDown)
            {
                var oldValue = (T)e.OldValue;
                var newValue = (T)e.NewValue;
                upDown.OnMinimumChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="Text"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnTextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is UpDownBase<T> upDown)
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
            if (e.Sender is UpDownBase<T> upDown)
            {
                var oldValue = (T)e.OldValue;
                var newValue = (T)e.NewValue;
                upDown.OnValueChanged(oldValue, newValue);
            }
        }

        private void SetValueInternal(T value)
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

        private static T OnCoerceMaximum(UpDownBase<T> upDown, T value)
        {
            return upDown.OnCoerceMaximum(value);
        }

        private static T OnCoerceMinimum(UpDownBase<T> upDown, T value)
        {
            return upDown.OnCoerceMinimum(value);
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

        internal void DoDecrement()
        {
            if (Spinner == null || (Spinner.ValidSpinDirection & ValidSpinDirections.Decrease) == ValidSpinDirections.Decrease)
            {
                OnDecrement();
            }
        }

        internal void DoIncrement()
        {
            if (Spinner == null || (Spinner.ValidSpinDirection & ValidSpinDirections.Increase) == ValidSpinDirections.Increase)
            {
                OnIncrement();
            }
        }

        public event EventHandler<SpinEventArgs> Spinned;

        private void TextBoxOnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.Device.Captured != Spinner)
            {
                Dispatcher.UIThread.InvokeAsync(() => { e.Device.Capture(Spinner); }, DispatcherPriority.Input);
            }
        }

        /// <summary>
        /// Defines the <see cref="ValueChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<UpDownValueChangedEventArgs<T>> ValueChangedEvent =
            RoutedEvent.Register<UpDownBase<T>, UpDownValueChangedEventArgs<T>>(nameof(ValueChanged), RoutingStrategies.Bubble);

        /// <summary>
        /// Raised when the <see cref="Value"/> changes.
        /// </summary>
        public event EventHandler<SpinEventArgs> ValueChanged
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
        protected bool SyncTextAndValueProperties(bool updateValueFromText, string text)
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
                    if (string.IsNullOrEmpty(text))
                    {
                        // An empty input sets the value to the default value.
                        SetValueInternal(DefaultValue);
                    }
                    else
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
                    // Don't replace the empty Text with the non-empty representation of DefaultValue.
                    var shouldKeepEmpty = !forceTextUpdate && string.IsNullOrEmpty(Text) && Equals(Value, DefaultValue) && !DisplayDefaultValueOnEmptyText;
                    if (!shouldKeepEmpty)
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
                    // repesents an invalid value. Disable the spinner in this case.
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
    }

    public class UpDownValueChangedEventArgs<T> : RoutedEventArgs
    {
        public UpDownValueChangedEventArgs(RoutedEvent routedEvent, T oldValue,  T newValue) : base(routedEvent)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; }
        public T NewValue { get; }
    }
}
