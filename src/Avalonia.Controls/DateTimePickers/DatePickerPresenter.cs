using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
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
    [TemplatePart(TemplateItems.AcceptButtonName,    typeof(Button), IsRequired = true)]
    [TemplatePart(TemplateItems.DayDownButtonName,   typeof(Button))]
    [TemplatePart(TemplateItems.DayHostName,         typeof(Panel), IsRequired = true)]
    [TemplatePart(TemplateItems.DaySelectorName,     typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(TemplateItems.DayUpButtonName,     typeof(Button))]
    [TemplatePart(TemplateItems.DismissButtonName,   typeof(Button))]
    [TemplatePart(TemplateItems.FirstSpacerName,     typeof(Control))]
    [TemplatePart(TemplateItems.MonthDownButtonName, typeof(Button))]
    [TemplatePart(TemplateItems.MonthHostName,       typeof(Panel), IsRequired = true)]
    [TemplatePart(TemplateItems.MonthSelectorName,   typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(TemplateItems.MonthUpButtonName,   typeof(Button))]
    [TemplatePart(TemplateItems.PickerContainerName, typeof(Grid), IsRequired = true)]
    [TemplatePart(TemplateItems.SecondSpacerName,    typeof(Control))]
    [TemplatePart(TemplateItems.YearDownButtonName,  typeof(Button))]
    [TemplatePart(TemplateItems.YearHostName,        typeof(Panel), IsRequired = true)]
    [TemplatePart(TemplateItems.YearSelectorName,    typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(TemplateItems.YearUpButtonName,    typeof(Button))]
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

        private struct TemplateItems
        {
            public Grid _pickerContainer;
            public const string PickerContainerName = "PART_PickerContainer";

            public Button _acceptButton;
            public const string AcceptButtonName = "PART_AcceptButton";

            public Button? _dismissButton;
            public const string DismissButtonName = "PART_DismissButton";

            public Control? _firstSpacer; 
            public const string FirstSpacerName = "PART_FirstSpacer";

            public Control? _secondSpacer;
            public const string SecondSpacerName = "PART_SecondSpacer";

            public Panel _monthHost; 
            public const string MonthHostName = "PART_MonthHost";

            public Panel _yearHost;
            public const string YearHostName = "PART_YearHost";

            public Panel _dayHost; 
            public const string DayHostName = "PART_DayHost";

            public DateTimePickerPanel _monthSelector;
            public const string MonthSelectorName = "PART_MonthSelector";

            public DateTimePickerPanel _yearSelector; 
            public const string YearSelectorName = "PART_YearSelector";

            public DateTimePickerPanel _daySelector;
            public const string DaySelectorName = "PART_DaySelector";

            public Button? _monthUpButton;
            public const string MonthUpButtonName = "PART_MonthUpButton";

            public Button? _dayUpButton; 
            public const string DayUpButtonName = "PART_DayUpButton";

            public Button? _yearUpButton; 
            public const string YearUpButtonName = "PART_YearUpButton";

            public Button? _monthDownButton; 
            public const string MonthDownButtonName = "PART_MonthDownButton";

            public Button? _dayDownButton; 
            public const string DayDownButtonName = "PART_DayDownButton";

            public Button? _yearDownButton;
            public const string YearDownButtonName = "PART_YearDownButton";
        }

        private TemplateItems? _templateItems;

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

            _templateItems = new()
            {
                // These are requirements, so throw if not found
                _pickerContainer = e.NameScope.Get<Grid>(TemplateItems.PickerContainerName),
                _monthHost = e.NameScope.Get<Panel>(TemplateItems.MonthHostName),
                _dayHost = e.NameScope.Get<Panel>(TemplateItems.DayHostName),
                _yearHost = e.NameScope.Get<Panel>(TemplateItems.YearHostName),

                _monthSelector = e.NameScope.Get<DateTimePickerPanel>(TemplateItems.MonthSelectorName),
                _daySelector = e.NameScope.Get<DateTimePickerPanel>(TemplateItems.DaySelectorName),
                _yearSelector = e.NameScope.Get<DateTimePickerPanel>(TemplateItems.YearSelectorName),

                _acceptButton = e.NameScope.Get<Button>(TemplateItems.AcceptButtonName),

                _monthUpButton = SelectorButton(TemplateItems.MonthUpButtonName, DateTimePickerPanelType.Month, SpinDirection.Decrease),
                _monthDownButton = SelectorButton(TemplateItems.MonthDownButtonName, DateTimePickerPanelType.Month, SpinDirection.Increase),
                _dayUpButton = SelectorButton(TemplateItems.DayUpButtonName, DateTimePickerPanelType.Day, SpinDirection.Decrease),
                _dayDownButton = SelectorButton(TemplateItems.DayDownButtonName, DateTimePickerPanelType.Day, SpinDirection.Increase),
                _yearUpButton = SelectorButton(TemplateItems.YearUpButtonName, DateTimePickerPanelType.Year, SpinDirection.Decrease),
                _yearDownButton = SelectorButton(TemplateItems.YearDownButtonName, DateTimePickerPanelType.Year, SpinDirection.Increase),

                _dismissButton = e.NameScope.Find<Button>(TemplateItems.DismissButtonName),
                _firstSpacer = e.NameScope.Find<Control>(TemplateItems.FirstSpacerName),
                _secondSpacer = e.NameScope.Find<Control>(TemplateItems.SecondSpacerName),
            };

            _templateItems.Value._acceptButton.Click += OnAcceptButtonClicked;
            _templateItems.Value._monthSelector.SelectionChanged += OnMonthChanged;
            _templateItems.Value._daySelector.SelectionChanged += OnDayChanged;
            _templateItems.Value._yearSelector.SelectionChanged += OnYearChanged;

            if (_templateItems.Value._dismissButton is { } dismissButton)
            {
                dismissButton.Click += OnDismissButtonClicked;
            }

            InitPicker();

            Button? SelectorButton(string name, DateTimePickerPanelType type, SpinDirection direction)
            {
                if (e.NameScope.Find<Button>(name) is { } button)
                {
                    button.Click += (s, e) => OnSelectorButtonClick(type, direction);
                    return button;
                }
                return null;
            }
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
            else if (change.Property == MonthFormatProperty || change.Property == YearFormatProperty || change.Property == DayFormatProperty)
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
            if (_templateItems is not { } items)
                return;

            _suppressUpdateSelection = true;

            items._monthSelector.MaximumValue = 12;
            items._monthSelector.MinimumValue = 1;
            items._monthSelector.ItemFormat = MonthFormat;

            items._daySelector.ItemFormat = DayFormat;

            items._yearSelector.MaximumValue = MaxYear.Year;
            items._yearSelector.MinimumValue = MinYear.Year;
            items._yearSelector.ItemFormat = YearFormat;

            SetGrid(items);

            // Date should've been set when we reach this point
            var dt = Date;
            if (DayVisible)
            {
                items._daySelector.FormatDate = dt.Date;
                var maxDays = _calendar.GetDaysInMonth(dt.Year, dt.Month);
                items._daySelector.MaximumValue = maxDays;
                items._daySelector.MinimumValue = 1;
                items._daySelector.SelectedValue = dt.Day;
            }

            if (MonthVisible)
            {
                items._monthSelector.SelectedValue = dt.Month;
                items._monthSelector.FormatDate = dt.Date;
            }

            if (YearVisible)
            {
                items._yearSelector.SelectedValue = dt.Year;
                items._yearSelector.FormatDate = dt.Date;
            }

            _suppressUpdateSelection = false;

            SetInitialFocus(items);
        }

        private void SetGrid(TemplateItems items)
        {
            var fmt = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var columns = new List<(Panel?, int)>
            {
                (items._monthHost, MonthVisible ? fmt.IndexOf("m", StringComparison.OrdinalIgnoreCase) : -1),
                (items._yearHost, YearVisible ? fmt.IndexOf("y", StringComparison.OrdinalIgnoreCase) : -1),
                (items._dayHost, DayVisible ? fmt.IndexOf("d", StringComparison.OrdinalIgnoreCase) : -1),
            };

            columns.Sort((x, y) => x.Item2 - y.Item2);
            items._pickerContainer.ColumnDefinitions.Clear();

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
                        items._pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    }

                    items._pickerContainer.ColumnDefinitions.Add(
                        new ColumnDefinition(column.Item1 == items._monthHost ? 138 : 78, GridUnitType.Star));

                    if (column.Item1.Parent is null)
                    {
                        items._pickerContainer.Children.Add(column.Item1);
                    }

                    Grid.SetColumn(column.Item1, (columnIndex++ * 2));
                }
            }

            ConfigureSpacer(items._firstSpacer, columnIndex > 1);
            ConfigureSpacer(items._secondSpacer, columnIndex > 2);

            static void ConfigureSpacer(Control? spacer, bool visible)
            {
                if (spacer == null)
                    return;

                // ternary conditional operator is used to make sure grid cells will be validated
                Grid.SetColumn(spacer, visible ? 1 : 0);
                spacer.IsVisible = visible;

            }
        }

        private void SetInitialFocus(TemplateItems items)
        {
            int monthCol = MonthVisible ? Grid.GetColumn(items._monthHost) : int.MaxValue;
            int dayCol = DayVisible ? Grid.GetColumn(items._dayHost) : int.MaxValue;
            int yearCol = YearVisible ? Grid.GetColumn(items._yearHost) : int.MaxValue;

            if (monthCol < dayCol && monthCol < yearCol)
            {
                items._monthSelector.Focus(NavigationMethod.Pointer);
            }
            else if (dayCol < monthCol && dayCol < yearCol)
            {
                items._monthSelector.Focus(NavigationMethod.Pointer);
            }
            else if (yearCol < monthCol && yearCol < dayCol)
            {
                items._yearSelector.Focus(NavigationMethod.Pointer);
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

        private void OnSelectorButtonClick(DateTimePickerPanelType type, SpinDirection direction)
        {
            var target = type switch
            {
                DateTimePickerPanelType.Month => _templateItems?._monthSelector,
                DateTimePickerPanelType.Day => _templateItems?._daySelector,
                DateTimePickerPanelType.Year=> _templateItems?._yearSelector,
                _ => throw new NotImplementedException(),
            };

            switch (direction)
            {
                case SpinDirection.Increase:
                    target?.ScrollDown();
                    break;
                case SpinDirection.Decrease:
                    target?.ScrollUp();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void OnYearChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection || _templateItems is not { } items)
                return;

            int maxDays = _calendar.GetDaysInMonth(items._yearSelector.SelectedValue, _syncDate.Month);
            var newDate = new DateTimeOffset(items._yearSelector.SelectedValue, _syncDate.Month,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            _syncDate = newDate;

            // We don't need to update the days if not displaying day, not february
            if (!DayVisible || _syncDate.Month != 2)
                return;

            _suppressUpdateSelection = true;

            items._daySelector.FormatDate = newDate.Date;

            if (items._daySelector.MaximumValue != maxDays)
                items._daySelector.MaximumValue = maxDays;
            else
                items._daySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        private void OnDayChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection || _templateItems is not { } items)
                return;
            _syncDate = new DateTimeOffset(_syncDate.Year, _syncDate.Month, items._daySelector.SelectedValue, 0, 0, 0, _syncDate.Offset);
        }

        private void OnMonthChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection || _templateItems is not { } items)
                return;

            int maxDays = _calendar.GetDaysInMonth(_syncDate.Year, items._monthSelector.SelectedValue);
            var newDate = new DateTimeOffset(_syncDate.Year, items._monthSelector.SelectedValue,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            if (!DayVisible)
            {
                _syncDate = newDate;
                return;
            }

            _suppressUpdateSelection = true;

            items._daySelector.FormatDate = newDate.Date;
            _syncDate = newDate;

            if (items._daySelector.MaximumValue != maxDays)
                items._daySelector.MaximumValue = maxDays;
            else
                items._daySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        internal double GetOffsetForPopup()
        {
            if (_templateItems is not { } items)
                return 0;

            var acceptDismissButtonHeight = items._acceptButton.Bounds.Height;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (items._monthSelector.ItemHeight / 2);
        }
    }
}
