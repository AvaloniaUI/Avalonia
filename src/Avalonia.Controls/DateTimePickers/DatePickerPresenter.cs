using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the presenter used for selecting a date. Intended for use with
    /// <see cref="DatePicker"/> but can be used independently. Combines
    /// DatePickerFlyout and DatePickerFlyoutPresenter
    /// </summary>
    public class DatePickerPresenter : PickerPresenterBase
    {
        public DatePickerPresenter()
        {
            var now = DateTimeOffset.Now;
            _minYear = new DateTimeOffset(now.Year - 100, 1, 1, 0, 0, 0, now.Offset);
            _maxYear = new DateTimeOffset(now.Year + 100, 12, 31, 0, 0, 0, now.Offset);
            _date = now;

            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        }

        /// <summary>
        /// Defines the <see cref="Date"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, DateTimeOffset> DateProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, DateTimeOffset>("Date", x => x.Date, (x, v) => x.Date = v);

        /// <summary>
        /// Defines the <see cref="DayFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, string> DayFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, string>("DayFormat", x => x.DayFormat, (x, v) => x.DayFormat = v);

        /// <summary>
        /// Defines the <see cref="DayVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, bool> DayVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, bool>("DayVisible", x => x.DayVisible, (x, v) => x.DayVisible = v);

        /// <summary>
        /// Defines the <see cref="MaxYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, DateTimeOffset> MaxYearProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, DateTimeOffset>("MaxYear", x => x.MaxYear, (x, v) => x.MaxYear = v);

        /// <summary>
        /// Defines the <see cref="MinYear"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, DateTimeOffset> MinYearProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, DateTimeOffset>("MinYear", x => x.MinYear, (x, v) => x.MinYear = v);

        /// <summary>
        /// Defines the <see cref="MonthFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, string> MonthFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, string>("MonthFormat", x => x.MonthFormat, (x, v) => x.MonthFormat = v);

        /// <summary>
        /// Defines the <see cref="MonthVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, bool> MonthVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, bool>("MonthVisible", x => x.MonthVisible, (x, v) => x.MonthVisible = v);

        /// <summary>
        /// Defines the <see cref="YearFormat"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, string> YearFormatProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, string>("YearFormat", x => x.YearFormat, (x, v) => x.YearFormat = v);

        /// <summary>
        /// Defines the <see cref="YearVisible"/> Property
        /// </summary>
        public static readonly DirectProperty<DatePickerPresenter, bool> YearVisibleProperty =
            AvaloniaProperty.RegisterDirect<DatePickerPresenter, bool>("YearVisible", x => x.YearVisible, (x, v) => x.YearVisible = v);

        //These aren't in WinUI
        /// <summary>
        /// Defines the <see cref="YearSelectorItemTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> YearSelectorItemTemplateProperty =
            AvaloniaProperty.Register<DatePickerPresenter, IDataTemplate>("YearSelectorItemTemplate");

        /// <summary>
        /// Defines the <see cref="MonthSelectorItemTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> MonthSelectorItemTemplateProperty =
            AvaloniaProperty.Register<DatePickerPresenter, IDataTemplate>("MonthSelectorItemTemplate");

        /// <summary>
        /// Defines the <see cref="DaySelectorItemTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> DaySelectorItemTemplateProperty =
            AvaloniaProperty.Register<DatePickerPresenter, IDataTemplate>("DaySelectorItemTemplate");

        /// <summary>
        /// Gets or sets the current Date for the picker
        /// </summary>
        public DateTimeOffset Date
        {
            get => _date;
            set => SetAndRaise(DateProperty, ref _date, value);
        }

        /// <summary>
        /// Gets or sets the DayFormat
        /// </summary>
        public string DayFormat
        {
            get => _dayFormat;
            set => SetAndRaise(DayFormatProperty, ref _dayFormat, value);
        }

        /// <summary>
        /// Get or sets whether the Day selector is visible
        /// </summary>
        public bool DayVisible
        {
            get => _dayVisible;
            set
            {
                SetAndRaise(DayVisibleProperty, ref _dayVisible, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum pickable year
        /// </summary>
        public DateTimeOffset MaxYear
        {
            get => _maxYear;
            set
            {
                SetAndRaise(MaxYearProperty, ref _maxYear, value);
                //Coerce date if needed
            }
        }

        /// <summary>
        /// Gets or sets the minimum pickable year
        /// </summary>
        public DateTimeOffset MinYear
        {
            get => _minYear;
            set
            {
                SetAndRaise(MinYearProperty, ref _minYear, value);
                //Coerce date if needed
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
        /// Gets or sets whether the month selector is visible
        /// </summary>
        public bool MonthVisible
        {
            get => _monthVisible;
            set
            {
                SetAndRaise(MonthVisibleProperty, ref _monthVisible, value);
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
        /// Gets or sets whether the year selector is visible
        /// </summary>
        public bool YearVisible
        {
            get => _yearVisible;
            set
            {
                SetAndRaise(YearVisibleProperty, ref _yearVisible, value);
            }
        }

        /// <summary>
        /// Gets or sets the item template for the YearSelector items
        /// </summary>
        public IDataTemplate YearSelectorItemTemplate
        {
            get => GetValue(YearSelectorItemTemplateProperty);
            set => SetValue(YearSelectorItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the item template for the MonthSelector items
        /// </summary>
        public IDataTemplate MonthSelectorItemTemplate
        {
            get => GetValue(MonthSelectorItemTemplateProperty);
            set => SetValue(MonthSelectorItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the item template for the DaySelector items
        /// </summary>
        public IDataTemplate DaySelectorItemTemplate
        {
            get => GetValue(DaySelectorItemTemplateProperty);
            set => SetValue(DaySelectorItemTemplateProperty, value);
        }

        /// <summary>
        /// Raised when the AcceptButton is clicked or Enter is pressed
        /// </summary>
        public event EventHandler<DatePickerValueChangedEventArgs> DatePicked;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            //This is a requirement, so throw if not found
            _pickerContainer = e.NameScope.Get<Grid>("PickerContainer");

            _acceptButton = e.NameScope.Find<Button>("AcceptButton");
            _dismissButton = e.NameScope.Find<Button>("DismissButton");
            _spacer1 = e.NameScope.Find<Rectangle>("FirstSpacer");
            _spacer2 = e.NameScope.Find<Rectangle>("SecondSpacer");

            if (_acceptButton != null)
            {
                _acceptButton.Click += OnAcceptButtonClicked;
            }
            if (_dismissButton != null)
            {
                _dismissButton.Click += OnDismissButtonClicked;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    _hostPopup.IsOpen = false;
                    e.Handled = true;
                    break;
                case Key.Tab:
                    var nextFocus = KeyboardNavigationHandler.GetNext(FocusManager.Instance.Current, NavigationDirection.Next);
                    KeyboardDevice.Instance?.SetFocusedElement(nextFocus, NavigationMethod.Tab, KeyModifiers.None);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    OnConfirmed();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }


        private void OnDismissButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _hostPopup.IsOpen = false;
        }

        private void OnAcceptButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OnConfirmed();
        }

        protected override void OnConfirmed()
        {
            var old = _date;
            OnDateChanged(new DatePickerValueChangedEventArgs(_initDate, Date));
            _hostPopup.IsOpen = false;
        }

        protected virtual void OnDateChanged(DatePickerValueChangedEventArgs args)
        {
            DatePicked?.Invoke(this, args);
        }

        /// <inheritdoc/>
        public override void ShowAt(Control target)
        {
            if (_hostPopup == null)
            {
                _hostPopup = new Avalonia.Controls.Primitives.Popup();
                _hostPopup.Child = this;
                _hostPopup.PlacementMode = PlacementMode.Bottom;
                _hostPopup.StaysOpen = true;
                ((ISetLogicalParent)_hostPopup).SetParent(target);
                _hostPopup.Closed += OnPopupClosed;
                _hostPopup.Opened += OnPopupOpened;
                _hostPopup.WindowManagerAddShadowHint = false;
                _hostPopup.Focusable = false;
            }

            if (target == null)
                throw new ArgumentNullException("Target cannot be null");

            _hostPopup.PlacementTarget = target;

            this.Width = target.Bounds.Width - 2;

            //Need to open the popup first, so the template is applied & our 
            //template items are available
            _hostPopup.IsOpen = true;

            var yearVis = YearVisible;
            var monthVis = MonthVisible;
            var dayVis = DayVisible;

            //Creates or destroys the selectors based on our settings
            EnsureSelectors(dayVis, monthVis, yearVis);

            //Set up the container grid
            SetGrid();

            //Focus the first Selector
            SetInitialFocus();

            _initDate = Date;

            //As we init the Selectors, prevent any selection behavior for occuring
            _suppressUpdateSelection = true;

            //If we're display a specific date component, create the items needed,
            //and set the selected index
            //If we're not using it & we previously had, make sure the items list is 
            //clear so we're not using unneeded memory
            if (dayVis)
            {
                CreateDayItems();
                _daySelector.Items = _dayItems;
                _daySelector.SelectedIndex = Date.Date.Day - 1;
            }
            else
            {
                if (_dayItems != null)
                {
                    _daySelector.Items = null;
                    _dayItems.Clear();
                    _dayItems = null;
                }
            }

            if (monthVis)
            {
                CreateMonthItems();
                _monthSelector.Items = _monthItems;
                _monthSelector.SelectedIndex = Date.Date.Month - 1;
            }
            else
            {
                if (_monthItems != null)
                {
                    _monthSelector.Items = null;
                    _monthItems.Clear();
                    _monthItems = null;
                }
            }

            if (yearVis)
            {
                CreateYearItems();
                _yearSelector.Items = _yearItems;
                _yearSelector.SelectedIndex = Date.Date.Year - MinYear.Date.Year;
            }
            else
            {
                if (_yearItems != null)
                {
                    _yearSelector.Items = null;
                    _yearItems.Clear();
                    _yearItems = null;
                }
            }

            _suppressUpdateSelection = false;
            OnOpened();
        }

        /// <summary>
        /// Ensures the selectors are created and setup
        /// </summary>
        private void EnsureSelectors(bool dayVis, bool monthVis, bool yearVis)
        {
            Contract.Requires<NullReferenceException>(_pickerContainer != null);

            if (yearVis && _yearSelector == null)
            {
                _yearSelector = new LoopingSelector();
                _pickerContainer.Children.Add(_yearSelector);
                _yearSelector.SelectionChanged += OnYearSelectionChanged;
                _yearSelector.ShouldLoop = false;
                _yearSelector.ItemTemplate = YearSelectorItemTemplate;
            }
            else if (!yearVis && _yearSelector != null)
            {
                _yearSelector.SelectionChanged -= OnYearSelectionChanged;
                if (_yearSelector.Parent != null)
                    _pickerContainer.Children.Remove(_yearSelector);
                _yearSelector.Items = null;
                _yearSelector = null;
            }

            if (monthVis && _monthSelector == null)
            {
                _monthSelector = new LoopingSelector();
                _pickerContainer.Children.Add(_monthSelector);
                _monthSelector.SelectionChanged += OnMonthSelectionChanged;
                _monthSelector.ShouldLoop = true;
                _monthSelector.ItemTemplate = MonthSelectorItemTemplate;
            }
            else if (!monthVis && _monthSelector != null)
            {
                _monthSelector.SelectionChanged -= OnMonthSelectionChanged;
                if (_monthSelector.Parent != null)
                    _pickerContainer.Children.Remove(_monthSelector);
                _monthSelector.Items = null;
                _monthSelector = null;
            }

            if (dayVis && _daySelector == null)
            {
                _daySelector = new LoopingSelector();
                _pickerContainer.Children.Add(_daySelector);
                _daySelector.SelectionChanged += OnDaySelectionChanged;
                _daySelector.ShouldLoop = true;
                _daySelector.ItemTemplate = DaySelectorItemTemplate;
            }
            else if (!dayVis && _daySelector != null)
            {
                _daySelector.SelectionChanged -= OnMonthSelectionChanged;
                if (_daySelector.Parent != null)
                    _pickerContainer.Children.Remove(_daySelector);
                _daySelector.Items = null;
                _daySelector = null;
            }
        }

        /// <summary>
        /// Creates the items for the day selector
        /// </summary>
        private void CreateDayItems()
        {
            if (_dayItems == null)
                _dayItems = new AvaloniaList<DatePickerPresenterItem>();

            if (_dayItems.Count > 0)
                _dayItems.Clear();

            var format = DayFormat;
            DateTimeFormatter formatter = new DateTimeFormatter(format);
            var date = Date;
            GregorianCalendar gc = new GregorianCalendar();
            var daysInMonth = gc.GetDaysInMonth(date.Date.Year, date.Date.Month);
            int dayIndex = 1;
            DateTimeOffset curDt;
            while (dayIndex <= daysInMonth)
            {
                curDt = new DateTimeOffset(date.Date.Year, date.Date.Month, dayIndex, 0, 0, 0, date.Offset);

                DatePickerPresenterItem dppi = new DatePickerPresenterItem(curDt);
                dppi.DisplayText = formatter.Format(curDt);
                _dayItems.Add(dppi);

                dayIndex++;
            }
        }

        /// <summary>
        /// Creates the items for the month selector
        /// </summary>
        private void CreateMonthItems()
        {
            if (_monthItems == null)
                _monthItems = new AvaloniaList<DatePickerPresenterItem>();

            if (_monthItems.Count > 0)
                _monthItems.Clear();

            var format = MonthFormat;
            DateTimeFormatter formatter = new DateTimeFormatter(format);
            var date = Date;
            int monthIndex = 1;
            DateTimeOffset curDt;
            while (monthIndex <= 12) //12 months in Gregorian Calendar
            {
                curDt = new DateTimeOffset(date.Date.Year, monthIndex, 1, 0, 0, 0, date.Offset);

                DatePickerPresenterItem dppi = new DatePickerPresenterItem(curDt);
                dppi.DisplayText = formatter.Format(curDt);
                _monthItems.Add(dppi);

                monthIndex++;
            }
        }

        /// <summary>
        /// Creates the items for the year selector
        /// </summary>
        private void CreateYearItems()
        {
            if (_yearItems == null)
                _yearItems = new AvaloniaList<DatePickerPresenterItem>();
            if (_yearItems.Count > 0)
                _yearItems.Clear();

            var format = YearFormat;
            DateTimeFormatter formatter = new DateTimeFormatter(format);
            var curDt = MinYear.Date;
            var max = MaxYear.Date;

            while (curDt <= max)
            {
                DatePickerPresenterItem dppi = new DatePickerPresenterItem(curDt);
                dppi.DisplayText = formatter.Format(curDt);
                _yearItems.Add(dppi);
                curDt = curDt.AddYears(1);
            }
        }

        /// <summary>
        /// Updates the dayitems list, if necessary, when the month or year changes
        /// </summary>
        private void UpdateDayItems(bool forceRecreate = false)
        {
            var date = Date;
            GregorianCalendar gc = new GregorianCalendar();
            var daysInMonth = gc.GetDaysInMonth(date.Date.Year, date.Date.Month);

            //Same number of days, no need to change unless forceRecreate == true
            if (!forceRecreate && daysInMonth == _dayItems.Count)
                return;

            DateTimeFormatter formatter = new DateTimeFormatter(DayFormat);
            if (forceRecreate)
            {
                //We're not actually going to recreate the entire list, we're just
                //going to update the Date & DisplayText of existing items
                //The logic below will handle adding/removing items, if needed...

                DateTimeOffset curDt = new DateTimeOffset(Date.Date.Year, Date.Date.Month, 1, 0, 0, 0, Date.Offset);
                for (int i = 0; i < _dayItems.Count; i++)
                {
                    _dayItems[i].UpdateStoredDate(curDt, formatter.Format(curDt));
                    curDt = curDt.AddDays(1);
                }
            }

            //Changes only occur at the end of the month, so don't recreate the entire
            //collection, just add/remove items where necessary;
            if (daysInMonth >= _dayItems.Count) //Add items
            {
                int dayIndex = _dayItems[_dayItems.Count - 1].GetStoredDate().Date.Day + 1;
                DateTimeOffset curDt;
                while (_dayItems.Count < daysInMonth)
                {
                    curDt = new DateTimeOffset(date.Date.Year, date.Date.Month, dayIndex, 0, 0, 0, date.Offset);
                    Debug.WriteLine($"ADDED {curDt}");
                    DatePickerPresenterItem dppi = new DatePickerPresenterItem(curDt);
                    dppi.DisplayText = formatter.Format(curDt);
                    _dayItems.Add(dppi);
                    dayIndex++;
                }
            }
            else //remove items
            {
                while (_dayItems.Count > daysInMonth)
                {
                    Debug.WriteLine($"REMOVED {_dayItems[_dayItems.Count - 1].GetStoredDate()}");
                    _dayItems.RemoveAt(_dayItems.Count - 1);
                }
            }

            //Make sure the SelectedIndex of the day selector is still valid
            if (_daySelector.SelectedIndex >= daysInMonth)
            {
                _daySelector.SelectedIndex = daysInMonth - 1;
            }

        }

        /// <summary>
        /// Sets the selector grid up, placing the selectors and spacers in the 
        /// correct location
        /// </summary>
        private void SetGrid()
        {
            Contract.Requires<NullReferenceException>(_pickerContainer != null);

            //Just like on DatePicker, brute force method to set grid
            //Display order of date is based on user's culture, we attempt
            //to figure out the normal date pattern
            //Not sure if better way exists, but it works...
            var fmt = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var monthfmt = Regex.Match(fmt, "(M|MM)");
            var yearfmt = Regex.Match(fmt, "(Y|YY|YYY|YYYY|y|yy|yyy|yyyy)");
            var dayfmt = Regex.Match(fmt, "(d|dd)");

            //Default is M-D-Y (en-us), this gives us fallback if pattern matching fails
            var monthIndex = 0;
            var yearIndex = 2;
            var dayIndex = 1;

            if (monthfmt.Success && yearfmt.Success && dayfmt.Success)
            {
                monthIndex = monthfmt.Index;
                yearIndex = yearfmt.Index;
                dayIndex = dayfmt.Index;
            }


            bool showMonth = MonthVisible;
            bool showDay = DayVisible;
            bool showYear = YearVisible;

            _pickerContainer.ColumnDefinitions.Clear();

            if (showMonth && !showDay && !showYear) //Month Only
            {
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(132, GridUnitType.Star));

                _monthSelector.IsVisible = true;
                if (_daySelector != null)
                    _daySelector.IsVisible = false;
                if (_yearSelector != null)
                    _yearSelector.IsVisible = false;

                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_monthSelector, 0);
            }
            else if (!showMonth && showDay && !showYear) //Day Only
            {
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(132, GridUnitType.Star));

                _daySelector.IsVisible = true;
                if (_monthSelector != null)
                    _monthSelector.IsVisible = false;
                if (_yearSelector != null)
                    _yearSelector.IsVisible = false;

                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_daySelector, 0);
            }
            else if (!showMonth && !showDay && showYear) //Year Only
            {
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(132, GridUnitType.Star));

                _yearSelector.IsVisible = true;
                if (_monthSelector != null)
                    _monthSelector.IsVisible = false;
                if (_daySelector != null)
                    _daySelector.IsVisible = false;

                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_yearSelector, 0);
            }
            else if (showMonth && showDay && !showYear) //Month and Day Only
            {
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(monthIndex < dayIndex ? 132 : 78, GridUnitType.Star));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(monthIndex < dayIndex ? 78 : 132, GridUnitType.Star));

                _monthSelector.IsVisible = true;
                _daySelector.IsVisible = true;
                if (_yearSelector != null)
                    _yearSelector.IsVisible = false;

                _spacer1.IsVisible = true;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_monthSelector, monthIndex < dayIndex ? 0 : 2);
                Grid.SetColumn(_daySelector, monthIndex < dayIndex ? 2 : 0);
                Grid.SetColumn(_spacer1, 1);
            }
            else if (showMonth && !showDay && showYear) //Month and Year Only
            {
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(monthIndex < yearIndex ? 132 : 78, GridUnitType.Star));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(monthIndex < yearIndex ? 78 : 132, GridUnitType.Star));

                _monthSelector.IsVisible = true;
                _yearSelector.IsVisible = true;
                if (_daySelector != null)
                    _daySelector.IsVisible = false;

                _spacer1.IsVisible = true;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_monthSelector, monthIndex < yearIndex ? 0 : 2);
                Grid.SetColumn(_yearSelector, monthIndex < yearIndex ? 2 : 0);
                Grid.SetColumn(_spacer1, 1);
            }
            else if (!showMonth && showDay && showYear) //Day and Year Only
            {
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(78, GridUnitType.Star));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(78, GridUnitType.Star));

                _daySelector.IsVisible = true;
                _yearSelector.IsVisible = true;
                if (_monthSelector != null)
                    _monthSelector.IsVisible = false;
                _spacer1.IsVisible = true;
                _spacer2.IsVisible = false;

                Grid.SetColumn(_yearSelector, dayIndex < yearIndex ? 2 : 0);
                Grid.SetColumn(_daySelector, dayIndex < yearIndex ? 0 : 2);
                Grid.SetColumn(_spacer1, 1);
            }
            else if (showMonth && showDay && showYear) //All Visible
            {
                bool isMonthFirst = monthIndex < dayIndex && monthIndex < yearIndex;
                bool isMonthSecond = (monthIndex > dayIndex && monthIndex < yearIndex) ||
                    (monthIndex < dayIndex && monthIndex > yearIndex);

                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(isMonthFirst ? 138 : 78, GridUnitType.Star));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(isMonthSecond ? 138 : 78, GridUnitType.Star));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                _pickerContainer.ColumnDefinitions.Add(new ColumnDefinition((!isMonthFirst && !isMonthSecond) ? 138 : 78, GridUnitType.Star));

                _monthSelector.IsVisible = true;
                _daySelector.IsVisible = true;
                _yearSelector.IsVisible = true;

                _spacer1.IsVisible = true;
                _spacer2.IsVisible = true;

                bool isDayFirst = !isMonthFirst && dayIndex < yearIndex;
                bool isDaySecond = (dayIndex > monthIndex && dayIndex < yearIndex) ||
                    (dayIndex < monthIndex && dayIndex > yearIndex);

                bool isYearFirst = !isDayFirst && !isMonthFirst;
                bool isYearSecond = (yearIndex > monthIndex && yearIndex < dayIndex) ||
                    (yearIndex < monthIndex && yearIndex > dayIndex);

                Grid.SetColumn(_monthSelector, isMonthFirst ? 0 : isMonthSecond ? 2 : 4);
                Grid.SetColumn(_yearSelector, isYearFirst ? 0 : (isMonthSecond || isDaySecond) ? 4 : 2);
                Grid.SetColumn(_daySelector, isDayFirst ? 0 : (isMonthSecond || isYearSecond) ? 4 : 2);

                Grid.SetColumn(_spacer1, 1);
                Grid.SetColumn(_spacer2, 3);
            }
            else
            {
                if (_monthSelector != null)
                    _monthSelector.IsVisible = false;
                if (_daySelector != null)
                    _daySelector.IsVisible = false;
                if (_yearSelector != null)
                    _yearSelector.IsVisible = false;

                _spacer1.IsVisible = false;
                _spacer2.IsVisible = false;
            }

            //Now to ensure we handle tab navigation correctly, we need to make sure the 
            //selectors are in order in the Grid's children collection so we navigate from
            //left to right
            if (MonthVisible && _monthSelector != null)
            {
                if (_pickerContainer.Children.IndexOf(_monthSelector) != Grid.GetColumn(_monthSelector))
                {
                    _pickerContainer.Children.Move(_pickerContainer.Children.IndexOf(_monthSelector), Grid.GetColumn(_monthSelector));
                }
            }
            if (DayVisible && _daySelector != null)
            {
                if (_pickerContainer.Children.IndexOf(_daySelector) != Grid.GetColumn(_daySelector))
                {
                    _pickerContainer.Children.Move(_pickerContainer.Children.IndexOf(_daySelector), Grid.GetColumn(_daySelector));
                }
            }
            if (YearVisible && _yearSelector != null)
            {
                if (_pickerContainer.Children.IndexOf(_yearSelector) != Grid.GetColumn(_yearSelector))
                {
                    _pickerContainer.Children.Move(_pickerContainer.Children.IndexOf(_yearSelector), Grid.GetColumn(_yearSelector));
                }
            }

        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            //TODO: figure out a dynamic way to set the offset
            //for now, hardcode what lines up with default behavior of 9 items displayed
            //This offset occurs AFTER adjustment for screen bounds occurs, so may need to adjust for that too
            _hostPopup.Host.ConfigurePosition(_hostPopup.PlacementTarget, PlacementMode.Top, new Point(1, -191.5));

        }

        private void OnPopupClosed(object sender, PopupClosedEventArgs e)
        {
            _hostPopup.PlacementTarget.Focus();
            KeyboardDevice.Instance?.SetFocusedElement(_hostPopup.PlacementTarget, NavigationMethod.Pointer, KeyModifiers.None);
            OnClosed();
        }

        /// <summary>
        /// Keeps the date in sync with the day selector, and updates the day selector if needed
        /// </summary>
        private void OnDaySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
            if (_suppressUpdateSelection)
                return;

            if (e.AddedItems[0] != null)
            {
                var currentDate = Date.Date;
                if (e.AddedItems[0] is DatePickerPresenterItem dppi)
                {
                    var itemDate = dppi.GetStoredDate();
                    Date = new DateTimeOffset(currentDate.Date.Year, currentDate.Date.Month, itemDate.Date.Day, 0, 0, 0, Date.Offset);
                }
            }

        }

        /// <summary>
        /// Keeps the date in sync with the month selector, and updates the day selector if needed
        /// </summary>
        private void OnMonthSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressUpdateSelection)
                return;
            _suppressUpdateSelection = true;
            if (e.AddedItems[0] != null)
            {
                var currentDate = Date.Date;
                if (e.AddedItems[0] is DatePickerPresenterItem dppi)
                {
                    var itemDate = dppi.GetStoredDate();

                    var day = currentDate.Date.Day;
                    int maxDays = new GregorianCalendar().GetDaysInMonth(currentDate.Date.Year, itemDate.Date.Month);
                    if (day > maxDays)
                        day = maxDays;

                    Date = new DateTimeOffset(currentDate.Date.Year, itemDate.Date.Month, day, 0, 0, 0, Date.Offset);
                    if (DayVisible && _daySelector != null)
                        UpdateDayItems(DayFormat.Contains("dayofweek") /*forceRecreate*/);
                }
            }
            _suppressUpdateSelection = false;
        }

        /// <summary>
        /// Keeps the date in sync with the year selector, and updates the day selector if needed
        /// </summary>
        private void OnYearSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
            if (_suppressUpdateSelection)
                return;

            if (e.AddedItems[0] != null)
            {
                var currentDate = Date.Date;
                if (e.AddedItems[0] is DatePickerPresenterItem dppi)
                {
                    var itemDate = dppi.GetStoredDate();

                    //Year selection changed also needs to update the day items, incase:
                    //Month is February (leap years)
                    //Day of week is displayed in DayFormat
                    //We can check for both of these here

                    if ((DayVisible && _daySelector != null) && Date.Date.Month == 2 || DayFormat.Contains("dayofweek"))
                    {
                        var day = currentDate.Date.Day;
                        int maxDays = new GregorianCalendar().GetDaysInMonth(itemDate.Date.Year, currentDate.Date.Month);
                        if (day > maxDays)
                            day = maxDays;
                        Date = new DateTimeOffset(itemDate.Date.Year, currentDate.Date.Month, day, 0, 0, 0, Date.Offset);

                        UpdateDayItems(true /*ForceRecreate*/);
                    }
                    else
                    {
                        Date = new DateTimeOffset(itemDate.Date.Year, currentDate.Date.Month, currentDate.Date.Day, 0, 0, 0, Date.Offset);
                    }

                }
            }
        }

        /// <summary>
        /// Forces selection on the leftmost selector 
        /// </summary>
        private void SetInitialFocus()
        {
            int monthCol = MonthVisible && _monthSelector != null ? Grid.GetColumn(_monthSelector) : int.MaxValue;
            int dayCol = DayVisible && _daySelector != null ? Grid.GetColumn(_daySelector) : int.MaxValue;
            int yearCol = YearVisible && _yearSelector != null ? Grid.GetColumn(_yearSelector) : int.MaxValue;

            if (monthCol < dayCol && monthCol < yearCol)
            {
                KeyboardDevice.Instance?.SetFocusedElement(_monthSelector, NavigationMethod.Pointer, KeyModifiers.None);
            }
            else if (dayCol < monthCol && dayCol < yearCol)
            {
                KeyboardDevice.Instance?.SetFocusedElement(_monthSelector, NavigationMethod.Pointer, KeyModifiers.None);
            }
            else if (yearCol < monthCol && yearCol < dayCol)
            {
                KeyboardDevice.Instance?.SetFocusedElement(_monthSelector, NavigationMethod.Pointer, KeyModifiers.None);
            }
        }

        //Item Lists
        private IList<DatePickerPresenterItem> _dayItems { get; set; }
        private IList<DatePickerPresenterItem> _monthItems { get; set; }
        private IList<DatePickerPresenterItem> _yearItems { get; set; }

        //Template Items
        private Grid _pickerContainer;
        private Avalonia.Controls.Button _acceptButton;
        private Avalonia.Controls.Button _dismissButton;
        private Rectangle _spacer1;
        private Rectangle _spacer2;

        //Selectors
        private LoopingSelector _yearSelector;
        private LoopingSelector _monthSelector;
        private LoopingSelector _daySelector;

        private DateTimeOffset _date;
        private string _dayFormat = "{day.integer}";
        private bool _dayVisible = true;
        private DateTimeOffset _maxYear;
        private DateTimeOffset _minYear;
        private string _monthFormat = "{month.full}";
        private bool _monthVisible = true;
        private string _yearFormat = "{year.full}";
        private bool _yearVisible = true;
        private DateTimeOffset _initDate;

        private bool _suppressUpdateSelection;

    }
}
