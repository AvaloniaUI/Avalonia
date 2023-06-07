using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the presenter used for selecting a date for a 
    /// <see cref="DatePicker"/>
    /// </summary>
    [TemplatePart("PART_AcceptButton",    typeof(Button))]
    [TemplatePart("PART_DayDownButton",   typeof(RepeatButton))]
    [TemplatePart("PART_DayHost",         typeof(Panel))]
    [TemplatePart("PART_DaySelector",     typeof(DateTimePickerPanel))]
    [TemplatePart("PART_DayUpButton",     typeof(RepeatButton))]
    [TemplatePart("PART_DismissButton",   typeof(Button))]
    [TemplatePart("PART_FirstSpacer",     typeof(Rectangle))]
    [TemplatePart("PART_MonthDownButton", typeof(RepeatButton))]
    [TemplatePart("PART_MonthHost",       typeof(Panel))]
    [TemplatePart("PART_MonthSelector",   typeof(DateTimePickerPanel))]
    [TemplatePart("PART_MonthUpButton",   typeof(RepeatButton))]
    [TemplatePart("PART_PickerContainer", typeof(Grid))]
    [TemplatePart("PART_SecondSpacer",    typeof(Rectangle))]
    [TemplatePart("PART_YearDownButton",  typeof(RepeatButton))]
    [TemplatePart("PART_YearHost",        typeof(Panel))]
    [TemplatePart("PART_YearSelector",    typeof(DateTimePickerPanel))]
    [TemplatePart("PART_YearUpButton",    typeof(RepeatButton))]
    public class DatePickerPresenter : PickerPresenterBase
    {
        /// <summary>
        /// Defines the <see cref="Date"/> Property
        /// </summary>
        public static readonly StyledProperty<DateTimeOffset> DateProperty =
            AvaloniaProperty.Register<DatePickerPresenter, DateTimeOffset>(nameof(Date), coerce: CoerceDate);

        private static DateTimeOffset CoerceDate(AvaloniaObject sender, DateTimeOffset value)
        {
            var max = sender.GetValue(MaxYearProperty);
            if (value > max)
            {
                return max;
            }
            var min = sender.GetValue(MinYearProperty);
            if (value < min)
            {
                return min;
            }

            return value;
        }

        /// <summary>
        /// Defines the <see cref="DayFormat"/> Property
        /// </summary>
        public static readonly StyledProperty<string> DayFormatProperty =
            DatePicker.DayFormatProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="DayVisible"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> DayVisibleProperty =
            DatePicker.DayVisibleProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="MaxYear"/> Property
        /// </summary>
        public static readonly StyledProperty<DateTimeOffset> MaxYearProperty =
            DatePicker.MaxYearProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="MinYear"/> Property
        /// </summary>
        public static readonly StyledProperty<DateTimeOffset> MinYearProperty =
            DatePicker.MinYearProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="MonthFormat"/> Property
        /// </summary>
        public static readonly StyledProperty<string> MonthFormatProperty =
            DatePicker.MonthFormatProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="MonthVisible"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> MonthVisibleProperty =
            DatePicker.MonthVisibleProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="YearFormat"/> Property
        /// </summary>
        public static readonly StyledProperty<string> YearFormatProperty =
            DatePicker.YearFormatProperty.AddOwner<DatePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="YearVisible"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> YearVisibleProperty =
            DatePicker.YearVisibleProperty.AddOwner<DatePickerPresenter>();

        // Template Items
        private Grid? _pickerContainer;
        private Button? _acceptButton;
        private Button? _dismissButton;
        private Rectangle? _spacer1;
        private Rectangle? _spacer2;
        private Panel? _monthHost;
        private Panel? _yearHost;
        private Panel? _dayHost;
        private DateTimePickerPanel? _monthSelector;
        private DateTimePickerPanel? _yearSelector;
        private DateTimePickerPanel? _daySelector;
        private Button? _monthUpButton;
        private Button? _dayUpButton;
        private Button? _yearUpButton;
        private Button? _monthDownButton;
        private Button? _dayDownButton;
        private Button? _yearDownButton;

        private DateTimeOffset _syncDate;

        private readonly GregorianCalendar _calendar;
        private bool _suppressUpdateSelection;

        public DatePickerPresenter()
        {
            var now = DateTimeOffset.Now;
            SetCurrentValue(MinYearProperty, new DateTimeOffset(now.Year - 100, 1, 1, 0, 0, 0, now.Offset));
            SetCurrentValue(MaxYearProperty, new DateTimeOffset(now.Year + 100, 12, 31, 0, 0, 0, now.Offset));
            SetCurrentValue(DateProperty, now);
            _calendar = new GregorianCalendar();
        }

        static DatePickerPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<DatePickerPresenter>(KeyboardNavigationMode.Cycle);
        }

        private static void OnDateRangeChanged(DatePickerPresenter sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender.CoerceValue(DateProperty);
        }

        /// <summary>
        /// Gets or sets the current Date for the picker
        /// </summary>
        public DateTimeOffset Date
        {
            get => GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        private void OnDateChanged(DateTimeOffset newValue)
        {
            _syncDate = newValue;
            InitPicker();
        }

        /// <summary>
        /// Gets or sets the DayFormat
        /// </summary>
        public string DayFormat
        {
            get => GetValue(DayFormatProperty);
            set => SetValue(DayFormatProperty, value);
        }

        /// <summary>
        /// Get or sets whether the Day selector is visible
        /// </summary>
        public bool DayVisible
        {
            get => GetValue(DayVisibleProperty);
            set => SetValue(DayVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum pickable year
        /// </summary>
        public DateTimeOffset MaxYear
        {
            get => GetValue(MaxYearProperty);
            set => SetValue(MaxYearProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum pickable year
        /// </summary>
        public DateTimeOffset MinYear
        {
            get => GetValue(MinYearProperty);
            set => SetValue(MinYearProperty, value);
        }

        /// <summary>
        /// Gets or sets the month format
        /// </summary>
        public string MonthFormat
        {
            get => GetValue(MonthFormatProperty);
            set => SetValue(MonthFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the month selector is visible
        /// </summary>
        public bool MonthVisible
        {
            get => GetValue(MonthVisibleProperty);
            set => SetValue(MonthVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the year format
        /// </summary>
        public string YearFormat
        {
            get => GetValue(YearFormatProperty);
            set => SetValue(YearFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the year selector is visible
        /// </summary>
        public bool YearVisible
        {
            get => GetValue(YearVisibleProperty);
            set => SetValue(YearVisibleProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            // These are requirements, so throw if not found
            _pickerContainer = e.NameScope.Get<Grid>("PART_PickerContainer");
            _monthHost = e.NameScope.Get<Panel>("PART_MonthHost");
            _dayHost = e.NameScope.Get<Panel>("PART_DayHost");
            _yearHost = e.NameScope.Get<Panel>("PART_YearHost");

            _monthSelector = e.NameScope.Get<DateTimePickerPanel>("PART_MonthSelector");
            _monthSelector.SelectionChanged += OnMonthChanged;

            _daySelector = e.NameScope.Get<DateTimePickerPanel>("PART_DaySelector");
            _daySelector.SelectionChanged += OnDayChanged;

            _yearSelector = e.NameScope.Get<DateTimePickerPanel>("PART_YearSelector");
            _yearSelector.SelectionChanged += OnYearChanged;

            _acceptButton = e.NameScope.Get<Button>("PART_AcceptButton");

            _monthUpButton = e.NameScope.Find<RepeatButton>("PART_MonthUpButton");
            if (_monthUpButton != null)
            {
                _monthUpButton.Click += OnSelectorButtonClick;
            }
            _monthDownButton = e.NameScope.Find<RepeatButton>("PART_MonthDownButton");
            if (_monthDownButton != null)
            {
                _monthDownButton.Click += OnSelectorButtonClick;
            }

            _dayUpButton = e.NameScope.Find<RepeatButton>("PART_DayUpButton");
            if (_dayUpButton != null)
            {
                _dayUpButton.Click += OnSelectorButtonClick;
            }
            _dayDownButton = e.NameScope.Find<RepeatButton>("PART_DayDownButton");
            if (_dayDownButton != null)
            {
                _dayDownButton.Click += OnSelectorButtonClick;
            }

            _yearUpButton = e.NameScope.Find<RepeatButton>("PART_YearUpButton");
            if (_yearUpButton != null)
            {
                _yearUpButton.Click += OnSelectorButtonClick;
            }
            _yearDownButton = e.NameScope.Find<RepeatButton>("PART_YearDownButton");
            if (_yearDownButton != null)
            {
                _yearDownButton.Click += OnSelectorButtonClick;
            }

            _dismissButton = e.NameScope.Find<Button>("PART_DismissButton");
            _spacer1 = e.NameScope.Find<Rectangle>("PART_FirstSpacer");
            _spacer2 = e.NameScope.Find<Rectangle>("PART_SecondSpacer");

            if (_acceptButton != null)
            {
                _acceptButton.Click += OnAcceptButtonClicked;
            }
            if (_dismissButton != null)
            {
                _dismissButton.Click += OnDismissButtonClicked;
            }
            InitPicker();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DateProperty)
            {
                OnDateChanged(change.GetNewValue<DateTimeOffset>());
            }
            else if (change.Property == MaxYearProperty || change.Property == MinYearProperty)
            {
                OnDateRangeChanged(this, change);
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
                    var focusManager = FocusManager.GetFocusManager(this);
                    if (focusManager?.GetFocusedElement() is { } focus)
                    {
                        var nextFocus = KeyboardNavigationHandler.GetNext(focus, NavigationDirection.Next);
                        nextFocus?.Focus(NavigationMethod.Tab);
                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                    SetCurrentValue(DateProperty, _syncDate);
                    OnConfirmed();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Initializes the picker selectors.
        /// </summary>
        private void InitPicker()
        {
            // OnApplyTemplate must've been called before we can init here...
            if (_pickerContainer == null)
                return;

            _suppressUpdateSelection = true;

            _monthSelector!.MaximumValue = 12;
            _monthSelector.MinimumValue = 1;
            _monthSelector.ItemFormat = MonthFormat;

            _daySelector!.ItemFormat = DayFormat;

            _yearSelector!.MaximumValue = MaxYear.Year;
            _yearSelector.MinimumValue = MinYear.Year;
            _yearSelector.ItemFormat = YearFormat;

            SetGrid();

            // Date should've been set when we reach this point
            var dt = Date;
            if (DayVisible)
            {
                _daySelector.FormatDate = dt.Date;
                var maxDays = _calendar.GetDaysInMonth(dt.Year, dt.Month);
                _daySelector.MaximumValue = maxDays;
                _daySelector.MinimumValue = 1;
                _daySelector.SelectedValue = dt.Day;
            }

            if (MonthVisible)
            {
                _monthSelector.SelectedValue = dt.Month;
                _monthSelector.FormatDate = dt.Date;
            }

            if (YearVisible)
            {
                _yearSelector.SelectedValue = dt.Year;
                _yearSelector.FormatDate = dt.Date;
            }

            _suppressUpdateSelection = false;

            SetInitialFocus();
        }

        private void SetGrid()
        {
            var fmt = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var columns = new List<(Panel?, int)>
            {
                (_monthHost, MonthVisible ? fmt.IndexOf("m", StringComparison.OrdinalIgnoreCase) : -1),
                (_yearHost, YearVisible ? fmt.IndexOf("y", StringComparison.OrdinalIgnoreCase) : -1),
                (_dayHost, DayVisible ? fmt.IndexOf("d", StringComparison.OrdinalIgnoreCase) : -1),
            };

            columns.Sort((x, y) => x.Item2 - y.Item2);
            _pickerContainer!.ColumnDefinitions.Clear();

            var columnIndex = 0;

            foreach (var column in columns)
            {
                if (column.Item1 is null)
                    continue;

                column.Item1.IsVisible = column.Item2 != -1;

                if (column.Item2 != -1)
                {
                    if (columnIndex > 0)
                    {
                        _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    }

                    _pickerContainer.ColumnDefinitions.Add(
                        new ColumnDefinition(column.Item1 == _monthHost ? 138 : 78, GridUnitType.Star));

                    if (column.Item1.Parent is null)
                    {
                        _pickerContainer.Children.Add(column.Item1);
                    }

                    Grid.SetColumn(column.Item1, (columnIndex++ * 2));
                }
            }

            var isSpacer1Visible = columnIndex > 1;
            var isSpacer2Visible = columnIndex > 2;
            // ternary conditional operator is used to make sure grid cells will be validated
            Grid.SetColumn(_spacer1!, isSpacer1Visible ? 1 : 0);
            Grid.SetColumn(_spacer2!, isSpacer2Visible ? 3 : 0);

            _spacer1!.IsVisible = isSpacer1Visible;
            _spacer2!.IsVisible = isSpacer2Visible;
        }

        private void SetInitialFocus()
        {
            int monthCol = MonthVisible ? Grid.GetColumn(_monthHost!) : int.MaxValue;
            int dayCol = DayVisible ? Grid.GetColumn(_dayHost!) : int.MaxValue;
            int yearCol = YearVisible ? Grid.GetColumn(_yearHost!) : int.MaxValue;

            if (monthCol < dayCol && monthCol < yearCol)
            {
                _monthSelector?.Focus(NavigationMethod.Pointer);
            }
            else if (dayCol < monthCol && dayCol < yearCol)
            {
                _monthSelector?.Focus(NavigationMethod.Pointer);
            }
            else if (yearCol < monthCol && yearCol < dayCol)
            {
                _yearSelector?.Focus(NavigationMethod.Pointer);
            }
        }

        private void OnDismissButtonClicked(object? sender, RoutedEventArgs e)
        {
            OnDismiss();
        }

        private void OnAcceptButtonClicked(object? sender, RoutedEventArgs e)
        {
            SetCurrentValue(DateProperty, _syncDate);
            OnConfirmed();
        }

        private void OnSelectorButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender == _monthUpButton)
                _monthSelector!.ScrollUp();
            else if (sender == _monthDownButton)
                _monthSelector!.ScrollDown();
            else if (sender == _yearUpButton)
                _yearSelector!.ScrollUp();
            else if (sender == _yearDownButton)
                _yearSelector!.ScrollDown();
            else if (sender == _dayUpButton)
                _daySelector!.ScrollUp();
            else if (sender == _dayDownButton)
                _daySelector!.ScrollDown();
        }

        private void OnYearChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection)
                return;

            int maxDays = _calendar.GetDaysInMonth(_yearSelector!.SelectedValue, _syncDate.Month);
            var newDate = new DateTimeOffset(_yearSelector.SelectedValue, _syncDate.Month,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            _syncDate = newDate;

            // We don't need to update the days if not displaying day, not february
            if (!DayVisible || _syncDate.Month != 2)
                return;

            _suppressUpdateSelection = true;

            _daySelector!.FormatDate = newDate.Date;

            if (_daySelector.MaximumValue != maxDays)
                _daySelector.MaximumValue = maxDays;
            else
                _daySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        private void OnDayChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection)
                return;
            _syncDate = new DateTimeOffset(_syncDate.Year, _syncDate.Month, _daySelector!.SelectedValue, 0, 0, 0, _syncDate.Offset);
        }

        private void OnMonthChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection)
                return;

            int maxDays = _calendar.GetDaysInMonth(_syncDate.Year, _monthSelector!.SelectedValue);
            var newDate = new DateTimeOffset(_syncDate.Year, _monthSelector.SelectedValue,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            if (!DayVisible)
            {
                _syncDate = newDate;
                return;
            }

            _suppressUpdateSelection = true;

            _daySelector!.FormatDate = newDate.Date;
            _syncDate = newDate;

            if (_daySelector.MaximumValue != maxDays)
                _daySelector.MaximumValue = maxDays;
            else
                _daySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        internal double GetOffsetForPopup()
        {
            if (_monthSelector is null)
                return 0;

            var acceptDismissButtonHeight = _acceptButton != null ? _acceptButton.Bounds.Height : 41;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (_monthSelector.ItemHeight / 2);
        }
    }
}
