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
    [TemplatePart("AcceptButton",     typeof(Button))]
    [TemplatePart("DismissButton",    typeof(Button))]
    [TemplatePart("HourDownButton",   typeof(RepeatButton))]
    [TemplatePart("HourSelector",     typeof(DateTimePickerPanel))]
    [TemplatePart("HourUpButton",     typeof(RepeatButton))]
    [TemplatePart("MinuteDownButton", typeof(RepeatButton))]
    [TemplatePart("MinuteSelector",   typeof(DateTimePickerPanel))]
    [TemplatePart("MinuteUpButton",   typeof(RepeatButton))]
    [TemplatePart("PeriodDownButton", typeof(RepeatButton))]
    [TemplatePart("PeriodHost",       typeof(Panel))]
    [TemplatePart("PeriodSelector",   typeof(DateTimePickerPanel))]
    [TemplatePart("PeriodUpButton",   typeof(RepeatButton))]
    [TemplatePart("PickerContainer",  typeof(Grid))]
    [TemplatePart("SecondSpacer",     typeof(Rectangle))]
    public class TimePickerPresenter : PickerPresenterBase
    {
        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> property
        /// </summary>
        public static readonly DirectProperty<TimePickerPresenter, int> MinuteIncrementProperty =
            TimePicker.MinuteIncrementProperty.AddOwner<TimePickerPresenter>(x => x.MinuteIncrement,
                (x, v) => x.MinuteIncrement = v);

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> property
        /// </summary>
        public static readonly DirectProperty<TimePickerPresenter, string> ClockIdentifierProperty =
            TimePicker.ClockIdentifierProperty.AddOwner<TimePickerPresenter>(x => x.ClockIdentifier,
                (x, v) => x.ClockIdentifier = v);

        /// <summary>
        /// Defines the <see cref="Time"/> property
        /// </summary>
        public static readonly DirectProperty<TimePickerPresenter, TimeSpan> TimeProperty =
            AvaloniaProperty.RegisterDirect<TimePickerPresenter, TimeSpan>(nameof(Time),
                x => x.Time, (x, v) => x.Time = v);

        public TimePickerPresenter()
        {
            Time = DateTime.Now.TimeOfDay;
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        }

        // TemplateItems
        private Grid? _pickerContainer;
        private Button? _acceptButton;
        private Button? _dismissButton;
        private Rectangle? _spacer2;
        private Panel? _periodHost;
        private DateTimePickerPanel? _hourSelector;
        private DateTimePickerPanel? _minuteSelector;
        private DateTimePickerPanel? _periodSelector;
        private Button? _hourUpButton;
        private Button? _minuteUpButton;
        private Button? _periodUpButton;
        private Button? _hourDownButton;
        private Button? _minuteDownButton;
        private Button? _periodDownButton;

        // Backing Fields
        private TimeSpan _time;
        private int _minuteIncrement = 1;
        private string _clockIdentifier = "12HourClock";

        /// <summary>
        /// Gets or sets the minute increment in the selector
        /// </summary>
        public int MinuteIncrement
        {
            get => _minuteIncrement;
            set
            {
                if (value < 1 || value > 59)
                    throw new ArgumentOutOfRangeException("1 >= MinuteIncrement <= 59");
                SetAndRaise(MinuteIncrementProperty, ref _minuteIncrement, value);
                InitPicker();
            }
        }

        /// <summary>
        /// Gets or sets the current clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {
            get => _clockIdentifier;
            set
            {
                if (string.IsNullOrEmpty(value) || !(value == "12HourClock" || value == "24HourClock"))
                    throw new ArgumentException("Invalid ClockIdentifier");
                SetAndRaise(ClockIdentifierProperty, ref _clockIdentifier, value);
                InitPicker();
            }
        }

        /// <summary>
        /// Gets or sets the current time
        /// </summary>
        public TimeSpan Time
        {
            get => _time;
            set
            {
                SetAndRaise(TimeProperty, ref _time, value);
                InitPicker();
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _pickerContainer = e.NameScope.Get<Grid>("PickerContainer");
            _periodHost = e.NameScope.Get<Panel>("PeriodHost");

            _hourSelector = e.NameScope.Get<DateTimePickerPanel>("HourSelector");
            _minuteSelector = e.NameScope.Get<DateTimePickerPanel>("MinuteSelector");
            _periodSelector = e.NameScope.Get<DateTimePickerPanel>("PeriodSelector");

            _spacer2 = e.NameScope.Get<Rectangle>("SecondSpacer");

            _acceptButton = e.NameScope.Get<Button>("AcceptButton");
            _acceptButton.Click += OnAcceptButtonClicked;

            _hourUpButton = e.NameScope.Find<RepeatButton>("HourUpButton");
            if (_hourUpButton != null)
                _hourUpButton.Click += OnSelectorButtonClick;
            _hourDownButton = e.NameScope.Find<RepeatButton>("HourDownButton");
            if (_hourDownButton != null)
                _hourDownButton.Click += OnSelectorButtonClick;

            _minuteUpButton = e.NameScope.Find<RepeatButton>("MinuteUpButton");
            if (_minuteUpButton != null)
                _minuteUpButton.Click += OnSelectorButtonClick;
            _minuteDownButton = e.NameScope.Find<RepeatButton>("MinuteDownButton");
            if (_minuteDownButton != null)
                _minuteDownButton.Click += OnSelectorButtonClick;

            _periodUpButton = e.NameScope.Find<RepeatButton>("PeriodUpButton");
            if (_periodUpButton != null)
                _periodUpButton.Click += OnSelectorButtonClick;
            _periodDownButton = e.NameScope.Find<RepeatButton>("PeriodDownButton");
            if (_periodDownButton != null)
                _periodDownButton.Click += OnSelectorButtonClick;

            _dismissButton = e.NameScope.Find<Button>("DismissButton");
            if (_dismissButton != null)
                _dismissButton.Click += OnDismissButtonClicked;

            InitPicker();
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
                    if (FocusManager.Instance?.Current is IInputElement focus)
                    {
                        var nextFocus = KeyboardNavigationHandler.GetNext(focus, NavigationDirection.Next);
                        KeyboardDevice.Instance?.SetFocusedElement(nextFocus, NavigationMethod.Tab, KeyModifiers.None);
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
            var per = _periodSelector!.SelectedValue;

            if (ClockIdentifier == "12HourClock")
            {
                hr = per == 1 ? (hr == 12) ? 12 : hr + 12 : per == 0 && hr == 12 ? 0 : hr;
            }

            Time = new TimeSpan(hr, min, 0);

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
            _minuteSelector.SelectedValue = Time.Minutes;
            _minuteSelector.ItemFormat = "mm";

            _periodSelector!.MaximumValue = 1;
            _periodSelector.MinimumValue = 0;
            _periodSelector.SelectedValue = hr >= 12 ? 1 : 0;

            SetGrid();
            KeyboardDevice.Instance?.SetFocusedElement(_hourSelector, NavigationMethod.Pointer, KeyModifiers.None);
        }

        private void SetGrid()
        {
            bool use24HourClock = ClockIdentifier == "24HourClock";

            var columnsD = use24HourClock ? "*, Auto, *" : "*, Auto, *, Auto, *";
            _pickerContainer!.ColumnDefinitions = new ColumnDefinitions(columnsD);

            _spacer2!.IsVisible = !use24HourClock;
            _periodHost!.IsVisible = !use24HourClock;

            Grid.SetColumn(_spacer2, use24HourClock ? 0 : 3);
            Grid.SetColumn(_periodHost, use24HourClock ? 0 : 4);
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
