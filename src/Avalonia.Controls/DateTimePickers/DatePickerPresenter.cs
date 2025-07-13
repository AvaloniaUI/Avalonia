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
    [TemplatePart(nameof(TemplateItems.PART_AcceptButton),    typeof(Button), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_DayDownButton),   typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_DayHost),         typeof(Panel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_DaySelector),     typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_DayUpButton),     typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_DismissButton),   typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_FirstSpacer),     typeof(Control))]
    [TemplatePart(nameof(TemplateItems.PART_MonthDownButton), typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_MonthHost),       typeof(Panel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_MonthSelector),   typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_MonthUpButton),   typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_PickerContainer), typeof(Grid), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_SecondSpacer),    typeof(Control))]
    [TemplatePart(nameof(TemplateItems.PART_YearDownButton),  typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_YearHost),        typeof(Panel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_YearSelector),    typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_YearUpButton),    typeof(Button))]
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
            public Grid PART_PickerContainer;
            public Button PART_AcceptButton;
            public Button? PART_DismissButton;
        
            public Control? PART_FirstSpacer;
            public Control? PART_SecondSpacer;
        
            public Panel PART_MonthHost;
            public Panel PART_YearHost;
            public Panel PART_DayHost;
        
            public DateTimePickerPanel PART_MonthSelector;
            public DateTimePickerPanel PART_YearSelector;
            public DateTimePickerPanel PART_DaySelector;
        
            public Button? PART_MonthUpButton;
            public Button? PART_DayUpButton;
            public Button? PART_YearUpButton;
            public Button? PART_MonthDownButton;
            public Button? PART_DayDownButton;
            public Button? PART_YearDownButton;
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
                PART_PickerContainer = e.NameScope.Get<Grid>(nameof(TemplateItems.PART_PickerContainer)),
                PART_MonthHost = e.NameScope.Get<Panel>(nameof(TemplateItems.PART_MonthHost)),
                PART_DayHost = e.NameScope.Get<Panel>(nameof(TemplateItems.PART_DayHost)),
                PART_YearHost = e.NameScope.Get<Panel>(nameof(TemplateItems.PART_YearHost)),

                PART_MonthSelector = e.NameScope.Get<DateTimePickerPanel>(nameof(TemplateItems.PART_MonthSelector)),
                PART_DaySelector = e.NameScope.Get<DateTimePickerPanel>(nameof(TemplateItems.PART_DaySelector)),
                PART_YearSelector = e.NameScope.Get<DateTimePickerPanel>(nameof(TemplateItems.PART_YearSelector)),

                PART_AcceptButton = e.NameScope.Get<Button>(nameof(TemplateItems.PART_AcceptButton)),

                PART_MonthUpButton = SelectorButton(nameof(TemplateItems.PART_MonthUpButton), DateTimePickerPanelType.Month, SpinDirection.Decrease),
                PART_MonthDownButton = SelectorButton(nameof(TemplateItems.PART_MonthDownButton), DateTimePickerPanelType.Month, SpinDirection.Increase),
                PART_DayUpButton = SelectorButton(nameof(TemplateItems.PART_DayUpButton), DateTimePickerPanelType.Day, SpinDirection.Decrease),
                PART_DayDownButton = SelectorButton(nameof(TemplateItems.PART_DayDownButton), DateTimePickerPanelType.Day, SpinDirection.Increase),
                PART_YearUpButton = SelectorButton(nameof(TemplateItems.PART_YearUpButton), DateTimePickerPanelType.Year, SpinDirection.Decrease),
                PART_YearDownButton = SelectorButton(nameof(TemplateItems.PART_YearDownButton), DateTimePickerPanelType.Year, SpinDirection.Increase),

                PART_DismissButton = e.NameScope.Find<Button>(nameof(TemplateItems.PART_DismissButton)),
                PART_FirstSpacer = e.NameScope.Find<Control>(nameof(TemplateItems.PART_FirstSpacer)),
                PART_SecondSpacer = e.NameScope.Find<Control>(nameof(TemplateItems.PART_SecondSpacer)),
            };

            _templateItems.Value.PART_AcceptButton.Click += OnAcceptButtonClicked;
            _templateItems.Value.PART_MonthSelector.SelectionChanged += OnMonthChanged;
            _templateItems.Value.PART_DaySelector.SelectionChanged += OnDayChanged;
            _templateItems.Value.PART_YearSelector.SelectionChanged += OnYearChanged;

            if (_templateItems.Value.PART_DismissButton is { } dismissButton)
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

            items.PART_MonthSelector.MaximumValue = 12;
            items.PART_MonthSelector.MinimumValue = 1;
            items.PART_MonthSelector.ItemFormat = MonthFormat;

            items.PART_DaySelector.ItemFormat = DayFormat;

            items.PART_YearSelector.MaximumValue = MaxYear.Year;
            items.PART_YearSelector.MinimumValue = MinYear.Year;
            items.PART_YearSelector.ItemFormat = YearFormat;

            SetGrid(items);

            // Date should've been set when we reach this point
            var dt = Date;
            if (DayVisible)
            {
                items.PART_DaySelector.FormatDate = dt.Date;
                var maxDays = _calendar.GetDaysInMonth(dt.Year, dt.Month);
                items.PART_DaySelector.MaximumValue = maxDays;
                items.PART_DaySelector.MinimumValue = 1;
                items.PART_DaySelector.SelectedValue = dt.Day;
            }

            if (MonthVisible)
            {
                items.PART_MonthSelector.SelectedValue = dt.Month;
                items.PART_MonthSelector.FormatDate = dt.Date;
            }

            if (YearVisible)
            {
                items.PART_YearSelector.SelectedValue = dt.Year;
                items.PART_YearSelector.FormatDate = dt.Date;
            }

            _suppressUpdateSelection = false;

            SetInitialFocus(items);
        }

        private void SetGrid(TemplateItems items)
        {
            var fmt = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var columns = new List<(Panel?, int)>
            {
                (items.PART_MonthHost, MonthVisible ? fmt.IndexOf("m", StringComparison.OrdinalIgnoreCase) : -1),
                (items.PART_YearHost, YearVisible ? fmt.IndexOf("y", StringComparison.OrdinalIgnoreCase) : -1),
                (items.PART_DayHost, DayVisible ? fmt.IndexOf("d", StringComparison.OrdinalIgnoreCase) : -1),
            };

            columns.Sort((x, y) => x.Item2 - y.Item2);
            items.PART_PickerContainer.ColumnDefinitions.Clear();

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
                        items.PART_PickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    }

                    items.PART_PickerContainer.ColumnDefinitions.Add(
                        new ColumnDefinition(column.Item1 == items.PART_MonthHost ? 138 : 78, GridUnitType.Star));

                    if (column.Item1.Parent is null)
                    {
                        items.PART_PickerContainer.Children.Add(column.Item1);
                    }

                    Grid.SetColumn(column.Item1, (columnIndex++ * 2));
                }
            }

            ConfigureSpacer(items.PART_FirstSpacer, columnIndex > 1);
            ConfigureSpacer(items.PART_SecondSpacer, columnIndex > 2);

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
            int monthCol = MonthVisible ? Grid.GetColumn(items.PART_MonthHost) : int.MaxValue;
            int dayCol = DayVisible ? Grid.GetColumn(items.PART_DayHost) : int.MaxValue;
            int yearCol = YearVisible ? Grid.GetColumn(items.PART_YearHost) : int.MaxValue;

            if (monthCol < dayCol && monthCol < yearCol)
            {
                items.PART_MonthSelector.Focus(NavigationMethod.Pointer);
            }
            else if (dayCol < monthCol && dayCol < yearCol)
            {
                items.PART_MonthSelector.Focus(NavigationMethod.Pointer);
            }
            else if (yearCol < monthCol && yearCol < dayCol)
            {
                items.PART_YearSelector.Focus(NavigationMethod.Pointer);
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
                DateTimePickerPanelType.Month => _templateItems?.PART_MonthSelector,
                DateTimePickerPanelType.Day => _templateItems?.PART_DaySelector,
                DateTimePickerPanelType.Year=> _templateItems?.PART_YearSelector,
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

            int maxDays = _calendar.GetDaysInMonth(items.PART_YearSelector.SelectedValue, _syncDate.Month);
            var newDate = new DateTimeOffset(items.PART_YearSelector.SelectedValue, _syncDate.Month,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            _syncDate = newDate;

            // We don't need to update the days if not displaying day, not february
            if (!DayVisible || _syncDate.Month != 2)
                return;

            _suppressUpdateSelection = true;

            items.PART_DaySelector.FormatDate = newDate.Date;

            if (items.PART_DaySelector.MaximumValue != maxDays)
                items.PART_DaySelector.MaximumValue = maxDays;
            else
                items.PART_DaySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        private void OnDayChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection || _templateItems is not { } items)
                return;
            _syncDate = new DateTimeOffset(_syncDate.Year, _syncDate.Month, items.PART_DaySelector.SelectedValue, 0, 0, 0, _syncDate.Offset);
        }

        private void OnMonthChanged(object? sender, EventArgs e)
        {
            if (_suppressUpdateSelection || _templateItems is not { } items)
                return;

            int maxDays = _calendar.GetDaysInMonth(_syncDate.Year, items.PART_MonthSelector.SelectedValue);
            var newDate = new DateTimeOffset(_syncDate.Year, items.PART_MonthSelector.SelectedValue,
                _syncDate.Day > maxDays ? maxDays : _syncDate.Day, 0, 0, 0, _syncDate.Offset);

            if (!DayVisible)
            {
                _syncDate = newDate;
                return;
            }

            _suppressUpdateSelection = true;

            items.PART_DaySelector.FormatDate = newDate.Date;
            _syncDate = newDate;

            if (items.PART_DaySelector.MaximumValue != maxDays)
                items.PART_DaySelector.MaximumValue = maxDays;
            else
                items.PART_DaySelector.RefreshItems();

            _suppressUpdateSelection = false;
        }

        internal double GetOffsetForPopup()
        {
            if (_templateItems is not { } items)
                return 0;

            var acceptDismissButtonHeight = items.PART_AcceptButton.Bounds.Height;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (items.PART_MonthSelector.ItemHeight / 2);
        }
    }
}
