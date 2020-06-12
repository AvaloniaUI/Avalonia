using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using System;
using System.Text.RegularExpressions;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control to allow the user to select a date
    /// </summary>
    public class DatePicker : TemplatedControl
    {
        public DatePicker()
        {
            PseudoClasses.Set(":hasnodate", true);
            _presenter = new DatePickerPresenter();
            var now = DateTimeOffset.Now;
            _minYear = new DateTimeOffset(now.Date.Year - 100, 1, 1, 0, 0, 0, now.Offset);
            _maxYear = new DateTimeOffset(now.Date.Year + 100, 12, 31, 0, 0, 0, now.Offset);

            _presenter.DatePicked += OnPresenterDatePicked;
        }

        /// <summary>
        /// Define the <see cref="DayFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, string> DayFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, string>("DayFormat",
                x => x.DayFormat, (x, v) => x.DayFormat = v);

        /// <summary>
        /// Defines the <see cref="DayVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, bool> DayVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, bool>("DayVisible",
                x => x.DayVisible, (x, v) => x.DayVisible = v);

        /// <summary>
        /// Defines the <see cref="Header"/> Property
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<DatePicker, object>("Header");

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<DatePicker, IDataTemplate>("HeaderTemplate");

        /// <summary>
        /// Defines the <see cref="MaxYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, DateTimeOffset> MaxYearProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, DateTimeOffset>("MaxYear", x => x.MaxYear, (x, v) => x.MaxYear = v);

        /// <summary>
        /// Defines the <see cref="MinYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, DateTimeOffset> MinYearProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, DateTimeOffset>("MinYear", x => x.MinYear, (x, v) => x.MinYear = v);

        /// <summary>
        /// Defines the <see cref="MonthFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, string> MonthFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, string>("MonthFormat", x => x.MonthFormat, (x, v) => x.MonthFormat = v);

        /// <summary>
        /// Defines the <see cref="MonthVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, bool> MonthVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, bool>("MonthVisible", x => x.MonthVisible, (x, v) => x.MonthVisible = v);

        /// <summary>
        /// Defiens the <see cref="YearFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, string> YearFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, string>("YearFormat", x => x.YearFormat, (x, v) => x.YearFormat = v);

        /// <summary>
        /// Defines the <see cref="YearVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, bool> YearVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, bool>("YearVisible", x => x.YearVisible, (x, v) => x.YearVisible = v);

        /// <summary>
        /// Defines the <see cref="SelectedDate"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePicker, DateTimeOffset?> SelectedDateProperty =
            AvaloniaProperty.RegisterDirect<DatePicker, DateTimeOffset?>("SelectedDate", x => x.SelectedDate, (x, v) => x.SelectedDate = v);

        /// <summary>
        /// Gets or sets the day format
        /// </summary>
        public string DayFormat
        {
            get => _dayFormat;
            set => SetAndRaise(DayFormatProperty, ref _dayFormat, value);
        }

        /// <summary>
        /// Gets or sets whether the day is visible
        /// </summary>
        public bool DayVisible
        {
            get => _dayVisible;
            set
            {
                SetAndRaise(DayVisibleProperty, ref _dayVisible, value);
                SetGrid();
            }
        }

        /// <summary>
        /// Gets or sets the DatePicker header
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the header template
        /// </summary>
        public IDataTemplate HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum year for the picker
        /// </summary>
        public DateTimeOffset MaxYear
        {
            get => _maxYear;
            set
            {
                if (value < MinYear)
                    throw new InvalidOperationException("MaxDate cannot be less than MinDate");
                SetAndRaise(MaxYearProperty, ref _maxYear, value);

                if (SelectedDate.HasValue && SelectedDate.Value > value)
                    SelectedDate = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum year for the picker
        /// </summary>
        public DateTimeOffset MinYear
        {
            get => _minYear;
            set
            {
                if (value > MaxYear)
                    throw new InvalidOperationException("MinDate cannot be greater than MaxDate");
                SetAndRaise(MinYearProperty, ref _minYear, value);

                if (SelectedDate.HasValue && SelectedDate.Value < value)
                    SelectedDate = value;
            }
        }

        /// <summary>
        /// Gets or sets the month format
        /// </summary>
        public string MonthFormat
        {
            get => _monthFormat;
            set => SetAndRaise(MonthFormatProperty, ref _monthFormat, value);
        }

        /// <summary>
        /// Gets or sets whether the month is visible
        /// </summary>
        public bool MonthVisible
        {
            get => _monthVisible;
            set
            {
                SetAndRaise(MonthVisibleProperty, ref _monthVisible, value);
                SetGrid();
            }
        }

        /// <summary>
        /// Gets or sets the year format
        /// </summary>
        public string YearFormat
        {
            get => _yearFormat;
            set => SetAndRaise(YearFormatProperty, ref _yearFormat, value);
        }

        /// <summary>
        /// Gets or sets whether the year is visible
        /// </summary>
        public bool YearVisible
        {
            get => _yearVisible;
            set
            {
                SetAndRaise(YearVisibleProperty, ref _yearVisible, value);
                SetGrid();
            }
        }

        /// <summary>
        /// Gets or sets the Selected Date for the picker, can be null
        /// </summary>
        public DateTimeOffset? SelectedDate
        {
            get => _selectedDate;
            set
            {
                var old = _selectedDate;
                SetAndRaise(SelectedDateProperty, ref _selectedDate, value);
                SetSelectedDateText();
                OnSelectedDateChanged(this, new DatePickerSelectedValueChangedEventArgs(old, value));
            }
        }

        /// <summary>
        /// Raised when the <see cref="SelectedDate"/> changes
        /// </summary>
        public event EventHandler<DatePickerSelectedValueChangedEventArgs> SelectedDateChanged;


        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _flyoutButton = e.NameScope.Find<Button>("FlyoutButton");
            _dayText = e.NameScope.Find<TextBlock>("DayText");
            _monthText = e.NameScope.Find<TextBlock>("MonthText");
            _yearText = e.NameScope.Find<TextBlock>("YearText");
            _container = e.NameScope.Find<Grid>("ButtonContentGrid");
            _spacer1 = e.NameScope.Find<Rectangle>("FirstSpacer");
            _spacer2 = e.NameScope.Find<Rectangle>("SecondSpacer");

            _areControlsAvailable = true;

            SetGrid();

            SetSelectedDateText();

            if (_flyoutButton != null)
            {
                _flyoutButton.Click += OnFlyoutButtonClicked;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Sets up the container grid and makes sure all label and spacers are placed correctly
        /// </summary>
        private void SetGrid()
        {
            //Brute force method to setup the container grid, probably a better way to do this
            //but it works...

            if (!_areControlsAvailable) //hopefully this never happens
                return;

            if (!_hasInit)
            {
                //Display order of date is based on user's culture, we attempt
                //to figure out the normal date pattern
                //TODO: Find better way to do this, but for now it works...
                var fmt = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                var monthfmt = Regex.Match(fmt, "(M|MM)");
                var yearfmt = Regex.Match(fmt, "(Y|YY|YYY|YYYY|y|yy|yyy|yyyy)");
                var dayfmt = Regex.Match(fmt, "(d|dd)");

                //Default is M-D-Y (en-us), this gives us fallback if pattern matching fails
                _monthIndex = 0;
                _yearIndex = 2;
                _dayIndex = 1;

                if (monthfmt.Success && yearfmt.Success && dayfmt.Success)
                {
                    _monthIndex = monthfmt.Index;
                    _yearIndex = yearfmt.Index;
                    _dayIndex = dayfmt.Index;
                }
                //Six possible combos, some probably don't actually exist, 
                //but prep for them anyway
                //M d y [x]
                //M y d [x]
                //d M y [x]
                //d y M [x]
                //y d M [x]
                //y M d [x]

                _hasInit = true;
            }

            bool showMonth = MonthVisible;
            bool showDay = DayVisible;
            bool showYear = YearVisible;

            _container.ColumnDefinitions.Clear();

            if (showMonth && !showDay && !showYear) //Month Only
            {
                _container.ColumnDefinitions.Add(new ColumnDefinition(132, GridUnitType.Star));
                _monthText.IsVisible = true;
                _dayText.IsVisible = false;
                _yearText.IsVisible = false;
                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_monthText, 0);
            }
            else if (!showMonth && showDay && !showYear) //Day Only
            {
                _container.ColumnDefinitions.Add(new ColumnDefinition(132, GridUnitType.Star));
                _monthText.IsVisible = false;
                _dayText.IsVisible = true;
                _yearText.IsVisible = false;
                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_dayText, 0);
            }
            else if (!showMonth && !showDay && showYear) //Year Only
            {
                _container.ColumnDefinitions.Add(new ColumnDefinition(132, GridUnitType.Star));
                _monthText.IsVisible = false;
                _dayText.IsVisible = false;
                _yearText.IsVisible = true;
                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_yearText, 0);
            }
            else if (showMonth && showDay && !showYear) //Month and Day Only
            {
                _container.ColumnDefinitions.Add(new ColumnDefinition(_monthIndex < _dayIndex ? 132 : 78, GridUnitType.Star));
                _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _container.ColumnDefinitions.Add(new ColumnDefinition(_monthIndex < _dayIndex ? 78 : 132, GridUnitType.Star));

                _monthText.IsVisible = true;
                _dayText.IsVisible = true;
                _yearText.IsVisible = false;
                _spacer1.IsVisible = true;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_monthText, _monthIndex < _dayIndex ? 0 : 2);
                Grid.SetColumn(_dayText, _monthIndex < _dayIndex ? 2 : 0);
                Grid.SetColumn(_spacer1, 1);
            }
            else if (showMonth && !showDay && showYear) //Month and Year Only
            {
                _container.ColumnDefinitions.Add(new ColumnDefinition(_monthIndex < _yearIndex ? 132 : 78, GridUnitType.Star));
                _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _container.ColumnDefinitions.Add(new ColumnDefinition(_monthIndex < _yearIndex ? 78 : 132, GridUnitType.Star));

                _monthText.IsVisible = true;
                _dayText.IsVisible = false;
                _yearText.IsVisible = true;
                _spacer1.IsVisible = true;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_monthText, _monthIndex < _yearIndex ? 0 : 2);
                Grid.SetColumn(_yearText, _monthIndex < _yearIndex ? 2 : 0);
                Grid.SetColumn(_spacer1, 1);
            }
            else if (!showMonth && showDay && showYear) //Day and Year Only
            {
                _container.ColumnDefinitions.Add(new ColumnDefinition(78, GridUnitType.Star));
                _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _container.ColumnDefinitions.Add(new ColumnDefinition(78, GridUnitType.Star));

                _monthText.IsVisible = false;
                _dayText.IsVisible = true;
                _yearText.IsVisible = true;
                _spacer1.IsVisible = true;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_yearText, _dayIndex < _yearIndex ? 2 : 0);
                Grid.SetColumn(_dayText, _dayIndex < _yearIndex ? 0 : 2);
                Grid.SetColumn(_spacer1, 1);
            }
            else if (showMonth && showDay && showYear) //All Visible
            {
                bool isMonthFirst = _monthIndex < _dayIndex && _monthIndex < _yearIndex;
                bool isMonthSecond = (_monthIndex > _dayIndex && _monthIndex < _yearIndex) ||
                    (_monthIndex < _dayIndex && _monthIndex > _yearIndex);

                _container.ColumnDefinitions.Add(new ColumnDefinition(isMonthFirst ? 138 : 78, GridUnitType.Star));
                _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _container.ColumnDefinitions.Add(new ColumnDefinition(isMonthSecond ? 138 : 78, GridUnitType.Star));
                _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _container.ColumnDefinitions.Add(new ColumnDefinition((!isMonthFirst && !isMonthSecond) ? 138 : 78, GridUnitType.Star));

                _monthText.IsVisible = true;
                _dayText.IsVisible = true;
                _yearText.IsVisible = true;
                _spacer1.IsVisible = true;
                _spacer2.IsVisible = true;

                bool isDayFirst = !isMonthFirst && _dayIndex < _yearIndex;
                bool isDaySecond = (_dayIndex > _monthIndex && _dayIndex < _yearIndex) ||
                    (_dayIndex < _monthIndex && _dayIndex > _yearIndex);

                bool isYearFirst = !isDayFirst && !isMonthFirst;
                bool isYearSecond = (_yearIndex > _monthIndex && _yearIndex < _dayIndex) ||
                    (_yearIndex < _monthIndex && _yearIndex > _dayIndex);

                Grid.SetColumn(_monthText, isMonthFirst ? 0 : isMonthSecond ? 2 : 4);
                Grid.SetColumn(_yearText, isYearFirst ? 0 : (isMonthSecond || isDaySecond) ? 4 : 2);
                Grid.SetColumn(_dayText, isDayFirst ? 0 : (isMonthSecond || isYearSecond) ? 4 : 2);

                Grid.SetColumn(_spacer1, 1);
                Grid.SetColumn(_spacer2, 3);
            }
            else
            {
                _monthText.IsVisible = false;
                _dayText.IsVisible = false;
                _yearText.IsVisible = false;
                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;
            }
        }

        /// <summary>
        /// Sets the TextBlocks when the SelectedDate changes
        /// </summary>
        private void SetSelectedDateText()
        {
            if (!_areControlsAvailable)
                return;

            if (SelectedDate.HasValue)
            {
                PseudoClasses.Set(":hasnodate", false);
                var selDate = SelectedDate.Value;
                _monthText.Text = new DateTimeFormatter(MonthFormat).Format(selDate);
                _yearText.Text = new DateTimeFormatter(YearFormat).Format(selDate);
                _dayText.Text = new DateTimeFormatter(DayFormat).Format(selDate);
            }
            else
            {
                PseudoClasses.Set(":hasnodate", true);
                _monthText.Text = "month";
                _yearText.Text = "year";
                _dayText.Text = "day";
            }
        }

        private void OnFlyoutButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _presenter.YearFormat = YearFormat;
            _presenter.DayFormat = DayFormat;
            _presenter.MonthFormat = MonthFormat;
            _presenter.MonthVisible = MonthVisible;
            _presenter.YearVisible = YearVisible;
            _presenter.DayVisible = DayVisible;
            //If SelectedDate hasn't been set, fallback to now
            _presenter.Date = SelectedDate.HasValue ? SelectedDate.Value : DateTimeOffset.Now;
            _presenter.MaxYear = MaxYear;
            _presenter.MinYear = MinYear;

            _presenter.ShowAt(this);
        }

        private void OnPresenterDatePicked(object sender, DatePickerValueChangedEventArgs args)
        {
            SelectedDate = args.NewDate;
        }

        protected virtual void OnSelectedDateChanged(object sender, DatePickerSelectedValueChangedEventArgs args)
        {
            SelectedDateChanged?.Invoke(sender, args);
        }


        //Template Items
        private Button _flyoutButton;
        private TextBlock _dayText;
        private TextBlock _monthText;
        private TextBlock _yearText;
        private Grid _container;
        private Rectangle _spacer1;
        private Rectangle _spacer2;

        private DatePickerPresenter _presenter;

        private bool _hasInit;
        private bool _areControlsAvailable;
        private int _monthIndex;
        private int _dayIndex;
        private int _yearIndex;

        private string _dayFormat = "{day.integer}";
        private bool _dayVisible = true;
        private DateTimeOffset _maxYear;
        private DateTimeOffset _minYear;
        private string _monthFormat = "{month.full}";
        private bool _monthVisible = true;
        private string _yearFormat = "{year.full}";
        private bool _yearVisible = true;
        private DateTimeOffset? _selectedDate;
    }
}
