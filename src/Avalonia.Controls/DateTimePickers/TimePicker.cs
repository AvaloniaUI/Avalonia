using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using System;
using System.Globalization;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control to allow the user to select a time.
    /// </summary>
    [TemplatePart("PART_FirstColumnDivider",      typeof(Rectangle))]
    [TemplatePart("PART_FirstPickerHost",         typeof(Border))]
    [TemplatePart("PART_FlyoutButton",            typeof(Button))]
    [TemplatePart("PART_FlyoutButtonContentGrid", typeof(Grid))]
    [TemplatePart("PART_HourTextBlock",           typeof(TextBlock))]
    [TemplatePart("PART_MinuteTextBlock",         typeof(TextBlock))]
    [TemplatePart("PART_PeriodTextBlock",         typeof(TextBlock))]
    [TemplatePart("PART_PickerPresenter",         typeof(TimePickerPresenter))]
    [TemplatePart("PART_Popup",                   typeof(Popup))]
    [TemplatePart("PART_SecondColumnDivider",     typeof(Rectangle))]
    [TemplatePart("PART_SecondPickerHost",        typeof(Border))]
    [TemplatePart("PART_ThirdPickerHost",         typeof(Border))]
    [PseudoClasses(":hasnotime")]
    public class TimePicker : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> property
        /// </summary>
        public static readonly StyledProperty<int> MinuteIncrementProperty =
            AvaloniaProperty.Register<TimePicker, int>(nameof(MinuteIncrement), 1, coerce: CoerceMinuteIncrement);

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> property
        /// </summary>
        public static readonly StyledProperty<string> ClockIdentifierProperty =
           AvaloniaProperty.Register<TimePicker, string>(nameof(ClockIdentifier), "12HourClock", coerce: CoerceClockIdentifier);

        /// <summary>
        /// Defines the <see cref="SelectedTime"/> property
        /// </summary>
        public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
            AvaloniaProperty.Register<TimePicker, TimeSpan?>(nameof(SelectedTime),
                defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

        // Template Items
        private TimePickerPresenter? _presenter;
        private Button? _flyoutButton;
        private Border? _firstPickerHost;
        private Border? _secondPickerHost;
        private Border? _thirdPickerHost;
        private TextBlock? _hourText;
        private TextBlock? _minuteText;
        private TextBlock? _periodText;
        private Rectangle? _firstSplitter;
        private Rectangle? _secondSplitter;
        private Grid? _contentGrid;
        private Popup? _popup;

        public TimePicker()
        {
            PseudoClasses.Set(":hasnotime", true);

            var timePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
            if (timePattern.IndexOf("H") != -1)
                SetCurrentValue(ClockIdentifierProperty, "24HourClock");
        }

        /// <summary>
        /// Gets or sets the minute increment in the picker
        /// </summary>
        public int MinuteIncrement
        {
            get => GetValue(MinuteIncrementProperty);
            set => SetValue(MinuteIncrementProperty, value);
        }

        private static int CoerceMinuteIncrement(AvaloniaObject sender, int value)
        {
            if (value < 1 || value > 59)
                throw new ArgumentOutOfRangeException(null, "1 >= MinuteIncrement <= 59");

            return value;
        }

        /// <summary>
        /// Gets or sets the clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {

            get => GetValue(ClockIdentifierProperty);
            set => SetValue(ClockIdentifierProperty, value);
        }

        private static string CoerceClockIdentifier(AvaloniaObject sender, string value)
        {
            if (!(string.IsNullOrEmpty(value) || value == "12HourClock" || value == "24HourClock"))
                throw new ArgumentException("Invalid ClockIdentifier", default(string));

            return value;
        }

        /// <summary>
        /// Gets or sets the selected time. Can be null.
        /// </summary>
        public TimeSpan? SelectedTime
        {
            get => GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        /// <summary>
        /// Raised when the <see cref="SelectedTime"/> property changes
        /// </summary>
        public event EventHandler<TimePickerSelectedValueChangedEventArgs>? SelectedTimeChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_flyoutButton != null)
                _flyoutButton.Click -= OnFlyoutButtonClicked;

            if (_presenter != null)
            {
                _presenter.Confirmed -= OnConfirmed;
                _presenter.Dismissed -= OnDismissPicker;
            }
            base.OnApplyTemplate(e);

            _flyoutButton = e.NameScope.Find<Button>("PART_FlyoutButton");

            _firstPickerHost = e.NameScope.Find<Border>("PART_FirstPickerHost");
            _secondPickerHost = e.NameScope.Find<Border>("PART_SecondPickerHost");
            _thirdPickerHost = e.NameScope.Find<Border>("PART_ThirdPickerHost");

            _hourText = e.NameScope.Find<TextBlock>("PART_HourTextBlock");
            _minuteText = e.NameScope.Find<TextBlock>("PART_MinuteTextBlock");
            _periodText = e.NameScope.Find<TextBlock>("PART_PeriodTextBlock");

            _firstSplitter = e.NameScope.Find<Rectangle>("PART_FirstColumnDivider");
            _secondSplitter = e.NameScope.Find<Rectangle>("PART_SecondColumnDivider");

            _contentGrid = e.NameScope.Find<Grid>("PART_FlyoutButtonContentGrid");

            _popup = e.NameScope.Find<Popup>("PART_Popup");
            _presenter = e.NameScope.Find<TimePickerPresenter>("PART_PickerPresenter");

            if (_flyoutButton != null)
                _flyoutButton.Click += OnFlyoutButtonClicked;

            SetGrid();
            SetSelectedTimeText();

            if (_presenter != null)
            {
                _presenter.Confirmed += OnConfirmed;
                _presenter.Dismissed += OnDismissPicker;

                _presenter[!TimePickerPresenter.MinuteIncrementProperty] = this[!MinuteIncrementProperty];
                _presenter[!TimePickerPresenter.ClockIdentifierProperty] = this[!ClockIdentifierProperty];
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MinuteIncrementProperty)
            {
                SetSelectedTimeText();
            }
            else if (change.Property == ClockIdentifierProperty)
            {
                SetGrid();
                SetSelectedTimeText();
            }
            else if (change.Property == SelectedTimeProperty)
            {
                var (oldValue, newValue) = change.GetOldAndNewValue<TimeSpan?>();
                OnSelectedTimeChanged(oldValue, newValue);
                SetSelectedTimeText();
            }
        }

        private void SetGrid()
        {
            if (_contentGrid == null)
                return;

            bool use24HourClock = ClockIdentifier == "24HourClock";

            var columnsD = use24HourClock ? "*, Auto, *" : "*, Auto, *, Auto, *";
            _contentGrid.ColumnDefinitions = new ColumnDefinitions(columnsD);

            _thirdPickerHost!.IsVisible = !use24HourClock;
            _secondSplitter!.IsVisible = !use24HourClock;

            Grid.SetColumn(_firstPickerHost!, 0);
            Grid.SetColumn(_secondPickerHost!, 2);

            Grid.SetColumn(_thirdPickerHost, use24HourClock ? 0 : 4);

            Grid.SetColumn(_firstSplitter!, 1);
            Grid.SetColumn(_secondSplitter, use24HourClock ? 0 : 3);
        }

        private void SetSelectedTimeText()
        {
            if (_hourText == null || _minuteText == null || _periodText == null)
                return;

            var time = SelectedTime;
            if (time.HasValue)
            {
                var newTime = SelectedTime!.Value;

                if (ClockIdentifier == "12HourClock")
                {
                    var hr = newTime.Hours;
                    hr = hr > 12 ? hr - 12 : hr == 0 ? 12 : hr;
                    newTime = new TimeSpan(hr, newTime.Minutes, 0);
                }

                _hourText.Text = newTime.ToString("%h");
                _minuteText.Text = newTime.ToString("mm");
                PseudoClasses.Set(":hasnotime", false);

                _periodText.Text = time.Value.Hours >= 12 ? TimeUtils.GetPMDesignator() : TimeUtils.GetAMDesignator();
            }
            else
            {
                _hourText.Text = "hour";
                _minuteText.Text = "minute";
                PseudoClasses.Set(":hasnotime", true);

                _periodText.Text = DateTime.Now.Hour >= 12 ?  TimeUtils.GetPMDesignator() :  TimeUtils.GetAMDesignator();
            }
        }

        protected virtual void OnSelectedTimeChanged(TimeSpan? oldTime, TimeSpan? newTime)
        {
            SelectedTimeChanged?.Invoke(this, new TimePickerSelectedValueChangedEventArgs(oldTime, newTime));
        }

        private void OnFlyoutButtonClicked(object? sender, Interactivity.RoutedEventArgs e)
        {
            if (_presenter == null)
                throw new InvalidOperationException("No DatePickerPresenter found.");
            if (_popup == null)
                throw new InvalidOperationException("No Popup found.");

            _presenter.Time = SelectedTime ?? DateTime.Now.TimeOfDay;

            _popup.Placement = PlacementMode.AnchorAndGravity;
            _popup.PlacementAnchor = Primitives.PopupPositioning.PopupAnchor.Bottom;
            _popup.PlacementGravity = Primitives.PopupPositioning.PopupGravity.Bottom;
            _popup.PlacementConstraintAdjustment = Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY;
            _popup.IsOpen = true;

            // Overlay popup hosts won't get measured until the next layout pass, but we need the
            // template to be applied to `_presenter` now. Detect this case and force a layout pass.
            if (!_presenter.IsMeasureValid)
                (VisualRoot as ILayoutRoot)?.LayoutManager?.ExecuteInitialLayoutPass();

            var deltaY = _presenter.GetOffsetForPopup();

            // The extra 5 px I think is related to default popup placement behavior
            _popup.VerticalOffset = deltaY + 5;
        }

        private void OnDismissPicker(object? sender, EventArgs e)
        {
            _popup!.Close();
            Focus();
        }

        private void OnConfirmed(object? sender, EventArgs e)
        {
            _popup!.Close();
            SetCurrentValue(SelectedTimeProperty, _presenter!.Time);
        }

        /// <summary>
        /// Clear <see cref="SelectedTime"/>.
        /// </summary>
        public void Clear()
        {
            SetCurrentValue(SelectedTimeProperty, null);
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
        {
            base.UpdateDataValidation(property, state, error);

            if (property == SelectedTimeProperty)
                DataValidationErrors.SetError(this, error);
        }
    }
}
