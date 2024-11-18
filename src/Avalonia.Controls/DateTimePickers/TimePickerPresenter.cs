using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the presenter used for selecting a time. Intended for use with
    /// <see cref="TimePicker"/> but can be used independently
    /// </summary>
    [TemplatePart("PART_AcceptButton",     typeof(Button), IsRequired = true)]
    [TemplatePart("PART_DismissButton",    typeof(Button))]
    [TemplatePart("PART_HourDownButton",   typeof(RepeatButton))]
    [TemplatePart("PART_HourSelector",     typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart("PART_HourUpButton",     typeof(RepeatButton))]
    [TemplatePart("PART_MinuteDownButton", typeof(RepeatButton))]
    [TemplatePart("PART_MinuteSelector",   typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart("PART_MinuteUpButton",   typeof(RepeatButton))]
    [TemplatePart("PART_SecondDownButton", typeof(RepeatButton))]
    [TemplatePart("PART_SecondHost",       typeof(Panel), IsRequired = true)]
    [TemplatePart("PART_SecondSelector",   typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart("PART_SecondUpButton",   typeof(RepeatButton))]
    [TemplatePart("PART_PeriodDownButton", typeof(RepeatButton))]
    [TemplatePart("PART_PeriodHost",       typeof(Panel), IsRequired = true)]
    [TemplatePart("PART_PeriodSelector",   typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart("PART_PeriodUpButton",   typeof(RepeatButton))]
    [TemplatePart("PART_PickerContainer",  typeof(Grid), IsRequired = true)]
    [TemplatePart("PART_SecondSpacer",     typeof(Rectangle), IsRequired = true)]
    [TemplatePart("PART_ThirdSpacer",      typeof(Rectangle), IsRequired = true)]
    public class TimePickerPresenter : PickerPresenterBase
    {
        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> property
        /// </summary>
        public static readonly StyledProperty<int> MinuteIncrementProperty =
            TimePicker.MinuteIncrementProperty.AddOwner<TimePickerPresenter>();
        
        /// <summary>
        /// Defines the <see cref="SecondIncrement"/> property
        /// </summary>
        public static readonly StyledProperty<int> SecondIncrementProperty =
            TimePicker.SecondIncrementProperty.AddOwner<TimePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> property
        /// </summary>
        public static readonly StyledProperty<string> ClockIdentifierProperty =
            TimePicker.ClockIdentifierProperty.AddOwner<TimePickerPresenter>();
        
        /// <summary>
        /// Defines the <see cref="UseSeconds"/> property
        /// </summary>
        public static readonly StyledProperty<bool> UseSecondsProperty =
            TimePicker.UseSecondsProperty.AddOwner<TimePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="Time"/> property
        /// </summary>
        public static readonly StyledProperty<TimeSpan> TimeProperty =
            AvaloniaProperty.Register<TimePickerPresenter, TimeSpan>(nameof(Time));

        public TimePickerPresenter()
        {
            SetCurrentValue(TimeProperty, DateTime.Now.TimeOfDay);
        }

        static TimePickerPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TimePickerPresenter>(KeyboardNavigationMode.Cycle);
        }

        // TemplateItems
        private Grid? _pickerContainer;
        private Button? _acceptButton;
        private Button? _dismissButton;
        private Rectangle? _spacer2;
        private Rectangle? _spacer3;
        private Panel? _secondHost;
        private Panel? _periodHost;
        private DateTimePickerPanel? _hourSelector;
        private DateTimePickerPanel? _minuteSelector;
        private DateTimePickerPanel? _secondSelector;
        private DateTimePickerPanel? _periodSelector;
        private Button? _hourUpButton;
        private Button? _minuteUpButton;
        private Button? _secondUpButton;
        private Button? _periodUpButton;
        private Button? _hourDownButton;
        private Button? _minuteDownButton;
        private Button? _secondDownButton;
        private Button? _periodDownButton;

        /// <summary>
        /// Gets or sets the minute increment in the selector
        /// </summary>
        public int MinuteIncrement
        {
            get => GetValue(MinuteIncrementProperty);
            set => SetValue(MinuteIncrementProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the second increment in the selector
        /// </summary>
        public int SecondIncrement
        {
            get => GetValue(SecondIncrementProperty);
            set => SetValue(SecondIncrementProperty, value);
        }

        /// <summary>
        /// Gets or sets the current clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {
            get => GetValue(ClockIdentifierProperty);
            set => SetValue(ClockIdentifierProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the current clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public bool UseSeconds
        {
            get => GetValue(UseSecondsProperty);
            set => SetValue(UseSecondsProperty, value);
        }

        /// <summary>
        /// Gets or sets the current time
        /// </summary>
        public TimeSpan Time
        {
            get => GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _pickerContainer = e.NameScope.Get<Grid>("PART_PickerContainer");
            _periodHost = e.NameScope.Get<Panel>("PART_PeriodHost");
            _secondHost = e.NameScope.Find<Panel>("PART_SecondHost");

            _hourSelector = e.NameScope.Get<DateTimePickerPanel>("PART_HourSelector");
            _minuteSelector = e.NameScope.Get<DateTimePickerPanel>("PART_MinuteSelector");
            _secondSelector = e.NameScope.Find<DateTimePickerPanel>("PART_SecondSelector");
            _periodSelector = e.NameScope.Get<DateTimePickerPanel>("PART_PeriodSelector");
            
            _spacer2 = e.NameScope.Get<Rectangle>("PART_SecondSpacer");
            _spacer3 = e.NameScope.Find<Rectangle>("PART_ThirdSpacer");

            _acceptButton = e.NameScope.Get<Button>("PART_AcceptButton");
            _acceptButton.Click += OnAcceptButtonClicked;

            _hourUpButton = e.NameScope.Find<RepeatButton>("PART_HourUpButton");
            if (_hourUpButton != null)
                _hourUpButton.Click += OnSelectorButtonClick;
            _hourDownButton = e.NameScope.Find<RepeatButton>("PART_HourDownButton");
            if (_hourDownButton != null)
                _hourDownButton.Click += OnSelectorButtonClick;

            _minuteUpButton = e.NameScope.Find<RepeatButton>("PART_MinuteUpButton");
            if (_minuteUpButton != null)
                _minuteUpButton.Click += OnSelectorButtonClick;
            _minuteDownButton = e.NameScope.Find<RepeatButton>("PART_MinuteDownButton");
            if (_minuteDownButton != null)
                _minuteDownButton.Click += OnSelectorButtonClick;
            
            _secondUpButton = e.NameScope.Find<RepeatButton>("PART_SecondUpButton");
            if (_secondUpButton != null)
                _secondUpButton.Click += OnSelectorButtonClick;
            _secondDownButton = e.NameScope.Find<RepeatButton>("PART_SecondDownButton");
            if (_secondDownButton != null)
                _secondDownButton.Click += OnSelectorButtonClick;

            _periodUpButton = e.NameScope.Find<RepeatButton>("PART_PeriodUpButton");
            if (_periodUpButton != null)
                _periodUpButton.Click += OnSelectorButtonClick;
            _periodDownButton = e.NameScope.Find<RepeatButton>("PART_PeriodDownButton");
            if (_periodDownButton != null)
                _periodDownButton.Click += OnSelectorButtonClick;

            _dismissButton = e.NameScope.Find<Button>("PART_DismissButton");
            if (_dismissButton != null)
                _dismissButton.Click += OnDismissButtonClicked;

            InitPicker();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MinuteIncrementProperty ||
                change.Property == SecondIncrementProperty ||
                change.Property == ClockIdentifierProperty ||
                change.Property == UseSecondsProperty ||
                change.Property == TimeProperty)
            {
                InitPicker();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    OnDismiss();
                    e.Handled = true;
                    break;
                case Key.Tab:
                    if (FocusManager.GetFocusManager(this)?.GetFocusedElement() is { } focus)
                    {
                        var nextFocus = KeyboardNavigationHandler.GetNext(focus, NavigationDirection.Next);
                        nextFocus?.Focus(NavigationMethod.Tab);
                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                    OnConfirmed();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnConfirmed()
        {
            var hr = _hourSelector!.SelectedValue;
            var min = _minuteSelector!.SelectedValue;
            var sec = _secondSelector?.SelectedValue ?? 0;
            var per = _periodSelector!.SelectedValue;

            if (ClockIdentifier == "12HourClock")
            {
                hr = per == 1 ? (hr == 12) ? 12 : hr + 12 : per == 0 && hr == 12 ? 0 : hr;
            }

            SetCurrentValue(TimeProperty, new TimeSpan(hr, min, UseSeconds ? sec : 0));

            base.OnConfirmed();
        }

        private void InitPicker()
        {
            if (_pickerContainer == null)
                return;

            bool clock12 = ClockIdentifier == "12HourClock";
            _hourSelector!.MaximumValue = clock12 ? 12 : 23;
            _hourSelector.MinimumValue = clock12 ? 1 : 0;
            _hourSelector.ItemFormat = "%h";
            var hr = Time.Hours;
            _hourSelector.SelectedValue = !clock12 ? hr :
                hr > 12 ? hr - 12 : hr == 0 ? 12 : hr;

            _minuteSelector!.MaximumValue = 59;
            _minuteSelector.MinimumValue = 0;
            _minuteSelector.Increment = MinuteIncrement;
            _minuteSelector.ItemFormat = "mm";
            _minuteSelector.SelectedValue = Time.Minutes;

            if (_secondSelector is not null)
            {
                _secondSelector.MaximumValue = 59;
                _secondSelector.MinimumValue = 0;
                _secondSelector.Increment = SecondIncrement;
                _secondSelector.ItemFormat = "ss";
                _secondSelector.SelectedValue = Time.Seconds;
            }

            _periodSelector!.MaximumValue = 1;
            _periodSelector.MinimumValue = 0;
            _periodSelector.SelectedValue = hr >= 12 ? 1 : 0;

            SetGrid();
            _hourSelector?.Focus(NavigationMethod.Pointer);
        }

        private void SetGrid()
        {
            var use24HourClock = ClockIdentifier == "24HourClock";
            var canUseSeconds = _secondHost is not null && _spacer3 is not null;
            
            var columnsD = new ColumnDefinitions();
            columnsD.Add(new ColumnDefinition(GridLength.Star));
            columnsD.Add(new ColumnDefinition(GridLength.Auto));
            columnsD.Add(new ColumnDefinition(GridLength.Star));
            if (canUseSeconds && UseSeconds)
            {
                columnsD.Add(new ColumnDefinition(GridLength.Auto));
                columnsD.Add(new ColumnDefinition(GridLength.Star));
            }
            if (!use24HourClock)
            {
                columnsD.Add(new ColumnDefinition(GridLength.Auto));
                columnsD.Add(new ColumnDefinition(GridLength.Star));
            }

            _pickerContainer!.ColumnDefinitions = columnsD;

            if (canUseSeconds)
            {
                _spacer2!.IsVisible = UseSeconds;
                _secondHost!.IsVisible = UseSeconds;
                _spacer3!.IsVisible = !use24HourClock;
                _periodHost!.IsVisible = !use24HourClock;

                var amPmColumn = (UseSeconds) ? 6 : 4;

                Grid.SetColumn(_spacer2, UseSeconds ? 3 : 0);
                Grid.SetColumn(_secondHost, UseSeconds ? 4 : 0);
                Grid.SetColumn(_spacer3, use24HourClock ? 0 : amPmColumn-1);
                Grid.SetColumn(_periodHost, use24HourClock ? 0 : amPmColumn);
            }
            else
            {
                _spacer2!.IsVisible = !use24HourClock;
                _periodHost!.IsVisible = !use24HourClock;
                Grid.SetColumn(_spacer2, use24HourClock ? 0 : 3);
                Grid.SetColumn(_periodHost, use24HourClock ? 0 : 4);
            }
        }

        private void OnDismissButtonClicked(object? sender, RoutedEventArgs e)
        {
            OnDismiss();
        }

        private void OnAcceptButtonClicked(object? sender, RoutedEventArgs e)
        {
            OnConfirmed();
        }

        private void OnSelectorButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender == _hourUpButton)
                _hourSelector!.ScrollUp();
            else if (sender == _hourDownButton)
                _hourSelector!.ScrollDown();
            else if (sender == _minuteUpButton)
                _minuteSelector!.ScrollUp();
            else if (sender == _minuteDownButton)
                _minuteSelector!.ScrollDown();
            else if (sender == _secondUpButton)
                _secondSelector!.ScrollUp();
            else if (sender == _secondDownButton)
                _secondSelector!.ScrollDown();
            else if (sender == _periodUpButton)
                _periodSelector!.ScrollUp();
            else if (sender == _periodDownButton)
                _periodSelector!.ScrollDown();
        }

        internal double GetOffsetForPopup()
        {
            if (_hourSelector is null)
                return 0;

            var acceptDismissButtonHeight = _acceptButton != null ? _acceptButton.Bounds.Height : 41;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (_hourSelector.ItemHeight / 2);
        }
    }
}
