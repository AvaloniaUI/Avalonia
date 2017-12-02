// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents the currently displayed month or year on a
    /// <see cref="T:Avalonia.Controls.Calendar" />.
    /// </summary>
    public sealed class CalendarItem : TemplatedControl
    {
        /// <summary>
        /// The number of days per week.
        /// </summary>
        private const int NumberOfDaysPerWeek = 7;

        private const string PART_ElementHeaderButton = "HeaderButton";
        private const string PART_ElementPreviousButton = "PreviousButton";
        private const string PART_ElementNextButton = "NextButton";
        private const string PART_ElementMonthView = "MonthView";
        private const string PART_ElementYearView = "YearView";

        private Button _headerButton;
        private Button _nextButton;
        private Button _previousButton;
        private Grid _monthView;
        private Grid _yearView;
        private ITemplate<IControl> _dayTitleTemplate;
        private CalendarButton _lastCalendarButton;
        private CalendarDayButton _lastCalendarDayButton;
        
        private DateTime _currentMonth;
        private bool _isMouseLeftButtonDown = false;
        private bool _isMouseLeftButtonDownYearView = false;
        private bool _isControlPressed = false;

        private System.Globalization.Calendar _calendar = new System.Globalization.GregorianCalendar();

        private PointerPressedEventArgs _downEventArg;
        private PointerPressedEventArgs _downEventArgYearView;

        internal Calendar Owner { get; set; }
        internal CalendarDayButton CurrentButton { get; set; }

        public static StyledProperty<IBrush> HeaderBackgroundProperty = Calendar.HeaderBackgroundProperty.AddOwner<CalendarItem>();
        public IBrush HeaderBackground
        {
            get { return GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }
        public static readonly DirectProperty<CalendarItem, ITemplate<IControl>> DayTitleTemplateProperty =
                AvaloniaProperty.RegisterDirect<CalendarItem, ITemplate<IControl>>(
                    nameof(DayTitleTemplate),
                    o => o.DayTitleTemplate,
                    (o,v) => o.DayTitleTemplate = v,
                    defaultBindingMode: BindingMode.OneTime);
        public ITemplate<IControl> DayTitleTemplate
        {
            get { return _dayTitleTemplate; }
            set { SetAndRaise(DayTitleTemplateProperty, ref _dayTitleTemplate, value); }
        }

        /// <summary>
        /// Gets the button that allows switching between month mode, year mode,
        /// and decade mode. 
        /// </summary>
        internal Button HeaderButton
        {
            get { return _headerButton; }
            private set
            {
                if (_headerButton != null)
                    _headerButton.Click -= HeaderButton_Click;

                _headerButton = value;

                if (_headerButton != null)
                {
                    _headerButton.Click += HeaderButton_Click;
                    _headerButton.Focusable = false;
                }
            }
        }
        /// <summary>
        /// Gets the button that displays the next page of the calendar when it
        /// is clicked.
        /// </summary>
        internal Button NextButton
        {
            get { return _nextButton; }
            private set
            {
                if (_nextButton != null)
                    _nextButton.Click -= NextButton_Click;

                _nextButton = value;

                if (_nextButton != null)
                {
                    // If the user does not provide a Content value in template,
                    // we provide a helper text that can be used in
                    // Accessibility this text is not shown on the UI, just used
                    // for Accessibility purposes
                    if (_nextButton.Content == null)
                    {
                        _nextButton.Content = "next button";
                    }

                    _nextButton.IsVisible = true;
                    _nextButton.Click += NextButton_Click;
                    _nextButton.Focusable = false;
                }
            }
        }
        /// <summary>
        /// Gets the button that displays the previous page of the calendar when
        /// it is clicked.
        /// </summary>
        internal Button PreviousButton
        {
            get { return _previousButton; }
            private set
            {
                if (_previousButton != null)
                    _previousButton.Click -= PreviousButton_Click;

                _previousButton = value;

                if (_previousButton != null)
                {
                    // If the user does not provide a Content value in template,
                    // we provide a helper text that can be used in
                    // Accessibility this text is not shown on the UI, just used
                    // for Accessibility purposes
                    if (_previousButton.Content == null)
                    {
                        _previousButton.Content = "previous button";
                    }

                    _previousButton.IsVisible = true;
                    _previousButton.Click += PreviousButton_Click;
                    _previousButton.Focusable = false;
                }
            }
        }

        /// <summary>
        /// Gets the Grid that hosts the content when in month mode.
        /// </summary>
        internal Grid MonthView
        {
            get { return _monthView; }
            private set
            {
                if (_monthView != null)
                    _monthView.PointerLeave -= MonthView_MouseLeave;

                _monthView = value;

                if (_monthView != null)
                    _monthView.PointerLeave += MonthView_MouseLeave;
            }
        }
        /// <summary>
        /// Gets the Grid that hosts the content when in year or decade mode.
        /// </summary>
        internal Grid YearView
        {
            get { return _yearView; }
            private set
            {
                if (_yearView != null)
                    _yearView.PointerLeave -= YearView_MouseLeave;

                _yearView = value;

                if (_yearView != null)
                    _yearView.PointerLeave += YearView_MouseLeave;
            }
        }

        private void PopulateGrids()
        {
            if (MonthView != null)
            {
                for (int i = 0; i < Calendar.RowsPerMonth; i++)
                {
                    if (_dayTitleTemplate != null)
                    {
                        var cell = _dayTitleTemplate.Build();
                        cell.DataContext = string.Empty;
                        cell.SetValue(Grid.RowProperty, 0);
                        cell.SetValue(Grid.ColumnProperty, i);
                        MonthView.Children.Add(cell);
                    }
                }

                for (int i = 1; i < Calendar.RowsPerMonth; i++)
                {
                    for (int j = 0; j < Calendar.ColumnsPerMonth; j++)
                    {
                        CalendarDayButton cell = new CalendarDayButton();

                        if (Owner != null)
                        {
                            cell.Owner = Owner;
                        }
                        cell.SetValue(Grid.RowProperty, i);
                        cell.SetValue(Grid.ColumnProperty, j);
                        cell.CalendarDayButtonMouseDown += Cell_MouseLeftButtonDown;
                        cell.CalendarDayButtonMouseUp += Cell_MouseLeftButtonUp;
                        cell.PointerEnter += Cell_MouseEnter;
                        cell.PointerLeave += Cell_MouseLeave;
                        cell.Click += Cell_Click;
                        MonthView.Children.Add(cell);
                    }
                }
            }

            if (YearView != null)
            {
                CalendarButton month;
                for (int i = 0; i < Calendar.RowsPerYear; i++)
                {
                    for (int j = 0; j < Calendar.ColumnsPerYear; j++)
                    {
                        month = new CalendarButton();

                        if (Owner != null)
                        {
                            month.Owner = Owner;
                        }
                        month.SetValue(Grid.RowProperty, i);
                        month.SetValue(Grid.ColumnProperty, j);
                        month.CalendarLeftMouseButtonDown += Month_CalendarButtonMouseDown;
                        month.CalendarLeftMouseButtonUp += Month_CalendarButtonMouseUp;
                        month.PointerEnter += Month_MouseEnter;
                        month.PointerLeave += Month_MouseLeave;
                        YearView.Children.Add(month);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the visual tree for the
        /// <see cref="T:System.Windows.Controls.Primitives.CalendarItem" />
        /// when a new template is applied.
        /// </summary>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            
            HeaderButton = e.NameScope.Find<Button>(PART_ElementHeaderButton);
            PreviousButton = e.NameScope.Find<Button>(PART_ElementPreviousButton);
            NextButton = e.NameScope.Find<Button>(PART_ElementNextButton);
            MonthView = e.NameScope.Find<Grid>(PART_ElementMonthView);
            YearView = e.NameScope.Find<Grid>(PART_ElementYearView);
            
            if (Owner != null)
            {
                UpdateDisabled(Owner.IsEnabled);
            }

            PopulateGrids();

            if (MonthView != null && YearView != null)
            {
                if (Owner != null)
                {
                    Owner.SelectedMonth = Owner.DisplayDateInternal;
                    Owner.SelectedYear = Owner.DisplayDateInternal;

                    if (Owner.DisplayMode == CalendarMode.Year)
                    {
                        UpdateYearMode();
                    }
                    else if (Owner.DisplayMode == CalendarMode.Decade)
                    {
                        UpdateDecadeMode();
                    }

                    if (Owner.DisplayMode == CalendarMode.Month)
                    {
                        UpdateMonthMode();
                        MonthView.IsVisible = true;
                        YearView.IsVisible = false;
                    }
                    else
                    {
                        YearView.IsVisible = true;
                        MonthView.IsVisible = false;
                    }
                }
                else
                {
                    UpdateMonthMode();
                    MonthView.IsVisible = true;
                    YearView.IsVisible = false;
                }
            }
        }

        private void SetDayTitles()
        {
            for (int childIndex = 0; childIndex < Calendar.ColumnsPerMonth; childIndex++)
            {
                var daytitle = MonthView.Children[childIndex];
                if (daytitle != null)
                {
                    if (Owner != null)
                    {
                        daytitle.DataContext = DateTimeHelper.GetCurrentDateFormat().ShortestDayNames[(childIndex + (int)Owner.FirstDayOfWeek) % NumberOfDaysPerWeek];
                    }
                    else
                    {
                        daytitle.DataContext = DateTimeHelper.GetCurrentDateFormat().ShortestDayNames[(childIndex + (int)DateTimeHelper.GetCurrentDateFormat().FirstDayOfWeek) % NumberOfDaysPerWeek];
                    }
                }
            }
        }
        /// <summary>
        /// How many days of the previous month need to be displayed.
        /// </summary>
        private int PreviousMonthDays(DateTime firstOfMonth)
        {
            DayOfWeek day = _calendar.GetDayOfWeek(firstOfMonth);
            int i;

            if (Owner != null)
            {
                i = ((day - Owner.FirstDayOfWeek + NumberOfDaysPerWeek) % NumberOfDaysPerWeek);
            }
            else
            {
                i = ((day - DateTimeHelper.GetCurrentDateFormat().FirstDayOfWeek + NumberOfDaysPerWeek) % NumberOfDaysPerWeek);
            }

            if (i == 0)
            {
                return NumberOfDaysPerWeek;
            }
            else
            {
                return i;
            }
        }

        internal void UpdateMonthMode()
        {
            if (Owner != null)
            {
                Debug.Assert(Owner.DisplayDate != null, "The Owner Calendar's DisplayDate should not be null!");
                _currentMonth = Owner.DisplayDateInternal;
            }
            else
            {
                _currentMonth = DateTime.Today;
            }

            if (_currentMonth != null)
            {
                SetMonthModeHeaderButton();
                SetMonthModePreviousButton(_currentMonth);
                SetMonthModeNextButton(_currentMonth);

                if (MonthView != null)
                {
                    SetDayTitles();
                    SetCalendarDayButtons(_currentMonth);
                }
            }
        }
        private void SetMonthModeHeaderButton()
        {
            if (HeaderButton != null)
            {
                if (Owner != null)
                {
                    HeaderButton.Content = Owner.DisplayDateInternal.ToString("Y", DateTimeHelper.GetCurrentDateFormat());
                    HeaderButton.IsEnabled = true;
                }
                else
                {
                    HeaderButton.Content = DateTime.Today.ToString("Y", DateTimeHelper.GetCurrentDateFormat());
                }
            }
        }
        private void SetMonthModeNextButton(DateTime firstDayOfMonth)
        {
            if (Owner != null && NextButton != null)
            {
                // DisplayDate is equal to DateTime.MaxValue
                if (DateTimeHelper.CompareYearMonth(firstDayOfMonth, DateTime.MaxValue) == 0)
                {
                    NextButton.IsEnabled = false;
                }
                else
                {
                    // Since we are sure DisplayDate is not equal to
                    // DateTime.MaxValue, it is safe to use AddMonths  
                    DateTime firstDayOfNextMonth = _calendar.AddMonths(firstDayOfMonth, 1);
                    NextButton.IsEnabled = (DateTimeHelper.CompareDays(Owner.DisplayDateRangeEnd, firstDayOfNextMonth) > -1);
                }
            }
        }
        private void SetMonthModePreviousButton(DateTime firstDayOfMonth)
        {
            if (Owner != null && PreviousButton != null)
            {
                PreviousButton.IsEnabled = (DateTimeHelper.CompareDays(Owner.DisplayDateRangeStart, firstDayOfMonth) < 0);
            }
        }

        private void SetButtonState(CalendarDayButton childButton, DateTime dateToAdd)
        {
            if (Owner != null)
            {
                childButton.Opacity = 1;

                // If the day is outside the DisplayDateStart/End boundary, do
                // not show it
                if (DateTimeHelper.CompareDays(dateToAdd, Owner.DisplayDateRangeStart) < 0 || DateTimeHelper.CompareDays(dateToAdd, Owner.DisplayDateRangeEnd) > 0)
                {
                    childButton.IsEnabled = false;
                    childButton.IsToday = false;
                    childButton.IsSelected = false;
                    childButton.Opacity = 0;
                }
                else
                {
                    // SET IF THE DAY IS SELECTABLE OR NOT
                    if (Owner.BlackoutDates.Contains(dateToAdd))
                    {
                        childButton.IsBlackout = true;
                    }
                    else
                    {
                        childButton.IsBlackout = false;
                    }
                    childButton.IsEnabled = true;

                    // SET IF THE DAY IS INACTIVE OR NOT: set if the day is a
                    // trailing day or not
                    childButton.IsInactive = (DateTimeHelper.CompareYearMonth(dateToAdd, Owner.DisplayDateInternal) != 0);

                    // SET IF THE DAY IS TODAY OR NOT
                    childButton.IsToday = (Owner.IsTodayHighlighted && dateToAdd == DateTime.Today);

                    // SET IF THE DAY IS SELECTED OR NOT
                    childButton.IsSelected = false;
                    foreach (DateTime item in Owner.SelectedDates)
                    {
                        // Since we should be comparing the Date values not
                        // DateTime values, we can't use
                        // Owner.SelectedDates.Contains(dateToAdd) directly
                        childButton.IsSelected |= (DateTimeHelper.CompareDays(dateToAdd, item) == 0);
                    }

                    // SET THE FOCUS ELEMENT
                    if (Owner.LastSelectedDate != null)
                    {
                        if (DateTimeHelper.CompareDays(Owner.LastSelectedDate.Value, dateToAdd) == 0)
                        {
                            if (Owner.FocusButton != null)
                            {
                                Owner.FocusButton.IsCurrent = false;
                            }
                            Owner.FocusButton = childButton;
                            if (Owner.HasFocusInternal)
                            {
                                Owner.FocusButton.IsCurrent = true;
                            }
                        }
                        else
                        {
                            childButton.IsCurrent = false;
                        }
                    }
                }
            }
        }
        private void SetCalendarDayButtons(DateTime firstDayOfMonth)
        {
            int lastMonthToDisplay = PreviousMonthDays(firstDayOfMonth);
            DateTime dateToAdd;

            if (DateTimeHelper.CompareYearMonth(firstDayOfMonth, DateTime.MinValue) > 0)
            {
                // DisplayDate is not equal to DateTime.MinValue we can subtract
                // days from the DisplayDate
                dateToAdd = _calendar.AddDays(firstDayOfMonth, -lastMonthToDisplay);
            }
            else
            {
                dateToAdd = firstDayOfMonth;
            }

            if (Owner != null && Owner.HoverEnd != null && Owner.HoverStart != null)
            {
                Owner.HoverEndIndex = null;
                Owner.HoverStartIndex = null;
            }

            int count = Calendar.RowsPerMonth * Calendar.ColumnsPerMonth;

            for (int childIndex = Calendar.ColumnsPerMonth; childIndex < count; childIndex++)
            {
                CalendarDayButton childButton = MonthView.Children[childIndex] as CalendarDayButton;
                Debug.Assert(childButton != null, "childButton should not be null!");

                childButton.Index = childIndex;
                SetButtonState(childButton, dateToAdd);

                // Update the indexes of hoverStart and hoverEnd
                if (Owner != null && Owner.HoverEnd != null && Owner.HoverStart != null)
                {
                    if (DateTimeHelper.CompareDays(dateToAdd, Owner.HoverEnd.Value) == 0)
                    {
                        Owner.HoverEndIndex = childIndex;
                    }

                    if (DateTimeHelper.CompareDays(dateToAdd, Owner.HoverStart.Value) == 0)
                    {
                        Owner.HoverStartIndex = childIndex;
                    }
                }

                //childButton.Focusable = false;
                childButton.Content = dateToAdd.Day.ToString(DateTimeHelper.GetCurrentDateFormat());
                childButton.DataContext = dateToAdd;

                if (DateTime.Compare((DateTime)DateTimeHelper.DiscardTime(DateTime.MaxValue), dateToAdd) > 0)
                {
                    // Since we are sure DisplayDate is not equal to
                    // DateTime.MaxValue, it is safe to use AddDays 
                    dateToAdd = _calendar.AddDays(dateToAdd, 1);
                }
                else
                {
                    // DisplayDate is equal to the DateTime.MaxValue, so there
                    // are no trailing days
                    childIndex++;
                    for (int i = childIndex; i < count; i++)
                    {
                        childButton = MonthView.Children[i] as CalendarDayButton;
                        Debug.Assert(childButton != null, "childButton should not be null!");
                        // button needs a content to occupy the necessary space
                        // for the content presenter
                        childButton.Content = i.ToString(DateTimeHelper.GetCurrentDateFormat());
                        childButton.IsEnabled = false;
                        childButton.Opacity = 0;
                    }
                    return;
                }
            }

            // If the HoverStart or HoverEndInternal could not be found on the
            // DisplayMonth set the values of the HoverStartIndex or
            // HoverEndIndex to be the first or last day indexes on the current
            // month
            if (Owner != null && Owner.HoverStart.HasValue && Owner.HoverEndInternal.HasValue)
            {
                if (!Owner.HoverEndIndex.HasValue)
                {
                    if (DateTimeHelper.CompareDays(Owner.HoverEndInternal.Value, Owner.HoverStart.Value) > 0)
                    {
                        Owner.HoverEndIndex = Calendar.ColumnsPerMonth * Calendar.RowsPerMonth - 1;
                    }
                    else
                    {
                        Owner.HoverEndIndex = Calendar.ColumnsPerMonth;
                    }
                }

                if (!Owner.HoverStartIndex.HasValue)
                {
                    if (DateTimeHelper.CompareDays(Owner.HoverEndInternal.Value, Owner.HoverStart.Value) > 0)
                    {
                        Owner.HoverStartIndex = Calendar.ColumnsPerMonth;
                    }
                    else
                    {
                        Owner.HoverStartIndex = Calendar.ColumnsPerMonth * Calendar.RowsPerMonth - 1;
                    }
                }
            }
        }

        internal void UpdateYearMode()
        {
            if (Owner != null)
            {
                Debug.Assert(Owner.SelectedMonth != null, "The Owner Calendar's SelectedMonth should not be null!");
                _currentMonth = (DateTime)Owner.SelectedMonth;
            }
            else
            {
                _currentMonth = DateTime.Today;
            }

            if (_currentMonth != null)
            {
                SetYearModeHeaderButton();
                SetYearModePreviousButton();
                SetYearModeNextButton();

                if (YearView != null)
                {
                    SetMonthButtonsForYearMode();
                }
            }
        }
        private void SetYearModeHeaderButton()
        {
            if (HeaderButton != null)
            {
                HeaderButton.IsEnabled = true;
                HeaderButton.Content = _currentMonth.Year.ToString(DateTimeHelper.GetCurrentDateFormat());
            }
        }
        private void SetYearModePreviousButton()
        {
            if (Owner != null && PreviousButton != null)
            {
                PreviousButton.IsEnabled = (Owner.DisplayDateRangeStart.Year != _currentMonth.Year);
            }
        }
        private void SetYearModeNextButton()
        {
            if (Owner != null && NextButton != null)
            {
                NextButton.IsEnabled = (Owner.DisplayDateRangeEnd.Year != _currentMonth.Year);
            }
        }

        private void SetMonthButtonsForYearMode()
        {
            int count = 0;
            foreach (object child in YearView.Children)
            {
                CalendarButton childButton = child as CalendarButton;
                Debug.Assert(childButton != null, "childButton should not be null!");
                // There should be no time component. Time is 12:00 AM
                DateTime day = new DateTime(_currentMonth.Year, count + 1, 1);
                childButton.DataContext = day;

                childButton.Content = DateTimeHelper.GetCurrentDateFormat().AbbreviatedMonthNames[count];
                childButton.IsVisible = true;

                if (Owner != null)
                {
                    if (day.Year == _currentMonth.Year && day.Month == _currentMonth.Month && day.Day == _currentMonth.Day)
                    {
                        Owner.FocusCalendarButton = childButton;
                        childButton.IsCalendarButtonFocused = Owner.HasFocusInternal;
                    }
                    else
                    {
                        childButton.IsCalendarButtonFocused = false;
                    }

                    Debug.Assert(Owner.DisplayDateInternal != null, "The Owner Calendar's DisplayDateInternal should not be null!");
                    childButton.IsSelected = (DateTimeHelper.CompareYearMonth(day, Owner.DisplayDateInternal) == 0);

                    if (DateTimeHelper.CompareYearMonth(day, Owner.DisplayDateRangeStart) < 0 || DateTimeHelper.CompareYearMonth(day, Owner.DisplayDateRangeEnd) > 0)
                    {
                        childButton.IsEnabled = false;
                        childButton.Opacity = 0;
                    }
                    else
                    {
                        childButton.IsEnabled = true;
                        childButton.Opacity = 1;
                    }
                }

                childButton.IsInactive = false;
                count++;
            }
        }
        internal void UpdateDecadeMode()
        {
            DateTime selectedYear;

            if (Owner != null)
            {
                Debug.Assert(Owner.SelectedYear != null, "The owning Calendar's selected year should not be null!");
                selectedYear = Owner.SelectedYear;
                _currentMonth = (DateTime)Owner.SelectedMonth;
            }
            else
            {
                _currentMonth = DateTime.Today;
                selectedYear = DateTime.Today;
            }

            if (_currentMonth != null)
            {
                int decade = DateTimeHelper.DecadeOfDate(selectedYear);
                int decadeEnd = DateTimeHelper.EndOfDecade(selectedYear);

                SetDecadeModeHeaderButton(decade, decadeEnd);
                SetDecadeModePreviousButton(decade);
                SetDecadeModeNextButton(decadeEnd);

                if (YearView != null)
                {
                    SetYearButtons(decade, decadeEnd);
                }
            }
        }
        internal void UpdateYearViewSelection(CalendarButton calendarButton)
        {
            if (Owner != null && calendarButton != null && calendarButton.DataContext != null)
            {
                Owner.FocusCalendarButton.IsCalendarButtonFocused = false;
                Owner.FocusCalendarButton = calendarButton;
                calendarButton.IsCalendarButtonFocused = Owner.HasFocusInternal;

                if (Owner.DisplayMode == CalendarMode.Year)
                {
                    Owner.SelectedMonth = (DateTime)calendarButton.DataContext;
                }
                else
                {
                    Owner.SelectedYear = (DateTime)calendarButton.DataContext;
                }
            }
        }

        private void SetYearButtons(int decade, int decadeEnd)
        {
            int year;
            int count = -1;
            foreach (object child in YearView.Children)
            {
                CalendarButton childButton = child as CalendarButton;
                Debug.Assert(childButton != null, "childButton should not be null!");
                year = decade + count;

                if (year <= DateTime.MaxValue.Year && year >= DateTime.MinValue.Year)
                {
                    // There should be no time component. Time is 12:00 AM
                    DateTime day = new DateTime(year, 1, 1);
                    childButton.DataContext = day;
                    childButton.Content = year.ToString(DateTimeHelper.GetCurrentDateFormat());
                    childButton.IsVisible = true;

                    if (Owner != null)
                    {
                        if (year == Owner.SelectedYear.Year)
                        {
                            Owner.FocusCalendarButton = childButton;
                            childButton.IsCalendarButtonFocused = Owner.HasFocusInternal;
                        }
                        else
                        {
                            childButton.IsCalendarButtonFocused = false;
                        }
                        childButton.IsSelected = (Owner.DisplayDate.Year == year);

                        if (year < Owner.DisplayDateRangeStart.Year || year > Owner.DisplayDateRangeEnd.Year)
                        {
                            childButton.IsEnabled = false;
                            childButton.Opacity = 0;
                        }
                        else
                        {
                            childButton.IsEnabled = true;
                            childButton.Opacity = 1;
                        }
                    }

                    // SET IF THE YEAR IS INACTIVE OR NOT: set if the year is a
                    // trailing year or not
                    childButton.IsInactive = (year < decade || year > decadeEnd);
                }
                else
                {
                    childButton.IsEnabled = false;
                    childButton.Opacity = 0;
                }

                count++;
            }
        }
        private void SetDecadeModeHeaderButton(int decade, int decadeEnd)
        {
            if (HeaderButton != null)
            {
                HeaderButton.Content = decade.ToString(CultureInfo.CurrentCulture) + "-" + decadeEnd.ToString(CultureInfo.CurrentCulture);
                HeaderButton.IsEnabled = false;
            }
        }
        private void SetDecadeModeNextButton(int decadeEnd)
        {
            if (Owner != null && NextButton != null)
            {
                NextButton.IsEnabled = (Owner.DisplayDateRangeEnd.Year > decadeEnd);
            }
        }
        private void SetDecadeModePreviousButton(int decade)
        {
            if (Owner != null && PreviousButton != null)
            {
                PreviousButton.IsEnabled = (decade > Owner.DisplayDateRangeStart.Year);
            }
        }

        internal void HeaderButton_Click(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                if (!Owner.HasFocusInternal)
                {
                    Owner.Focus();
                }
                Button b = sender as Button;
                DateTime d;

                if (b.IsEnabled)
                {
                    if (Owner.DisplayMode == CalendarMode.Month)
                    {
                        if (Owner.DisplayDate != null)
                        {
                            d = Owner.DisplayDateInternal;
                            Owner.SelectedMonth = new DateTime(d.Year, d.Month, 1);
                        }
                        Owner.DisplayMode = CalendarMode.Year;
                    }
                    else
                    {
                        Debug.Assert(Owner.DisplayMode == CalendarMode.Year, "The Owner Calendar's DisplayMode should be Year!");

                        if (Owner.SelectedMonth != null)
                        {
                            d = Owner.SelectedMonth;
                            Owner.SelectedYear = new DateTime(d.Year, d.Month, 1);
                        }
                        Owner.DisplayMode = CalendarMode.Decade;
                    }
                }
            }
        }
        internal void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                if (!Owner.HasFocusInternal)
                {
                    Owner.Focus();
                }

                Button b = sender as Button;
                if (b.IsEnabled)
                {
                    Owner.OnPreviousClick();
                }
            }
        }
        internal void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                if (!Owner.HasFocusInternal)
                {
                    Owner.Focus();
                }
                Button b = sender as Button;

                if (b.IsEnabled)
                {
                    Owner.OnNextClick();
                }
            }
        }

        internal void Cell_MouseEnter(object sender, PointerEventArgs e)
        {
            if (Owner != null)
            {
                CalendarDayButton b = sender as CalendarDayButton;
                if (_isMouseLeftButtonDown && b != null && b.IsEnabled && !b.IsBlackout)
                {
                    // Update the states of all buttons to be selected starting
                    // from HoverStart to b
                    switch (Owner.SelectionMode)
                    {
                        case CalendarSelectionMode.SingleDate:
                            {
                                DateTime selectedDate = (DateTime)b.DataContext;
                                Owner.DatePickerDisplayDateFlag = true;
                                if (Owner.SelectedDates.Count == 0)
                                {
                                    Owner.SelectedDates.Add(selectedDate);
                                }
                                else
                                {
                                    Owner.SelectedDates[0] = selectedDate;
                                }
                                return;
                            }
                        case CalendarSelectionMode.SingleRange:
                        case CalendarSelectionMode.MultipleRange:
                            {
                                Debug.Assert(b.DataContext != null, "The DataContext should not be null!");
                                Owner.UnHighlightDays();
                                Owner.HoverEndIndex = b.Index;
                                Owner.HoverEnd = (DateTime)b.DataContext;
                                // Update the States of the buttons
                                Owner.HighlightDays();
                                return;
                            }
                    }
                }
            }
        }
        internal void Cell_MouseLeave(object sender, PointerEventArgs e)
        {
            if (_isMouseLeftButtonDown)
            {
                CalendarDayButton b = sender as CalendarDayButton;
                // The button is in Pressed state. Change the state to normal.
                if (e.Device.Captured == b)
                    e.Device.Capture(null);
                // null check is added for unit tests
                if (_downEventArg != null)
                {
                    var arg =
                        new PointerReleasedEventArgs()
                        {
                            Device = _downEventArg.Device,
                            MouseButton = _downEventArg.MouseButton,
                            Handled = _downEventArg.Handled,
                            InputModifiers = _downEventArg.InputModifiers,
                            Route = _downEventArg.Route,
                            Source = _downEventArg.Source
                        };

                    b.SendMouseLeftButtonUp(arg);
                }
                _lastCalendarDayButton = b;
            }
        }
        internal void Cell_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (Owner != null)
            {
                if (!Owner.HasFocusInternal)
                {
                    Owner.Focus();
                }

                bool ctrl, shift;
                CalendarExtensions.GetMetaKeyState(e.InputModifiers, out ctrl, out shift);
                CalendarDayButton b = sender as CalendarDayButton;

                if (b != null)
                {
                    _isControlPressed = ctrl;
                    if (b.IsEnabled && !b.IsBlackout)
                    {
                        DateTime selectedDate = (DateTime)b.DataContext;
                        Debug.Assert(selectedDate != null, "selectedDate should not be null!");
                        _isMouseLeftButtonDown = true;
                        // null check is added for unit tests
                        if (e != null)
                        {
                            _downEventArg = e;
                        }

                        switch (Owner.SelectionMode)
                        {
                            case CalendarSelectionMode.None:
                                {
                                    return;
                                }
                            case CalendarSelectionMode.SingleDate:
                                {
                                    Owner.DatePickerDisplayDateFlag = true;
                                    if (Owner.SelectedDates.Count == 0)
                                    {
                                        Owner.SelectedDates.Add(selectedDate);
                                    }
                                    else
                                    {
                                        Owner.SelectedDates[0] = selectedDate;
                                    }
                                    return;
                                }
                            case CalendarSelectionMode.SingleRange:
                                {
                                    // Set the start or end of the selection
                                    // range
                                    if (shift)
                                    {
                                        Owner.UnHighlightDays();
                                        Owner.HoverEnd = selectedDate;
                                        Owner.HoverEndIndex = b.Index;
                                        Owner.HighlightDays();
                                    }
                                    else
                                    {
                                        Owner.UnHighlightDays();
                                        Owner.HoverStart = selectedDate;
                                        Owner.HoverStartIndex = b.Index;
                                    }
                                    return;
                                }
                            case CalendarSelectionMode.MultipleRange:
                                {
                                    if (shift)
                                    {
                                        if (!ctrl)
                                        {
                                            // clear the list, set the states to
                                            // default
                                            foreach (DateTime item in Owner.SelectedDates)
                                            {
                                                Owner.RemovedItems.Add(item);
                                            }
                                            Owner.SelectedDates.ClearInternal();
                                        }
                                        Owner.HoverEnd = selectedDate;
                                        Owner.HoverEndIndex = b.Index;
                                        Owner.HighlightDays();
                                    }
                                    else
                                    {
                                        if (!ctrl)
                                        {
                                            // clear the list, set the states to
                                            // default
                                            foreach (DateTime item in Owner.SelectedDates)
                                            {
                                                Owner.RemovedItems.Add(item);
                                            }
                                            Owner.SelectedDates.ClearInternal();
                                            Owner.UnHighlightDays();
                                        }
                                        Owner.HoverStart = selectedDate;
                                        Owner.HoverStartIndex = b.Index;
                                    }
                                    return;
                                }
                        }
                    }
                    else
                    {
                        // If a click occurs on a BlackOutDay we set the
                        // HoverStart to be null
                        Owner.HoverStart = null;
                    }
                }
                else
                {
                    _isControlPressed = false;
                }
            }
        }
        private void AddSelection(CalendarDayButton b)
        {
            if (Owner != null)
            {
                Owner.HoverEndIndex = b.Index;
                Owner.HoverEnd = (DateTime)b.DataContext;

                if (Owner.HoverEnd != null && Owner.HoverStart != null)
                {
                    // this is selection with Mouse, we do not guarantee the
                    // range does not include BlackOutDates.  AddRange method
                    // will throw away the BlackOutDates based on the
                    // SelectionMode
                    Owner.IsMouseSelection = true;
                    Owner.SelectedDates.AddRange(Owner.HoverStart.Value, Owner.HoverEnd.Value);
                    Owner.OnDayClick((DateTime)b.DataContext);
                }
            }
        }
        internal void Cell_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
        {
            if (Owner != null)
            {
                CalendarDayButton b = sender as CalendarDayButton;
                if (b != null && !b.IsBlackout)
                {
                    Owner.OnDayButtonMouseUp(e);
                }
                _isMouseLeftButtonDown = false;
                if (b != null && b.DataContext != null)
                {
                    if (Owner.SelectionMode == CalendarSelectionMode.None || Owner.SelectionMode == CalendarSelectionMode.SingleDate)
                    {
                        Owner.OnDayClick((DateTime)b.DataContext);
                        return;
                    }
                    if (Owner.HoverStart.HasValue)
                    {
                        switch (Owner.SelectionMode)
                        {
                            case CalendarSelectionMode.SingleRange:
                                {
                                    // Update SelectedDates
                                    foreach (DateTime item in Owner.SelectedDates)
                                    {
                                        Owner.RemovedItems.Add(item);
                                    }
                                    Owner.SelectedDates.ClearInternal();
                                    AddSelection(b);
                                    return;
                                }
                            case CalendarSelectionMode.MultipleRange:
                                {
                                    // add the selection (either single day or
                                    // SingleRange day)
                                    AddSelection(b);
                                    return;
                                }
                        }
                    }
                    else
                    {
                        // If the day is Disabled but a trailing day we should
                        // be able to switch months
                        if (b.IsInactive && b.IsBlackout)
                        {
                            Owner.OnDayClick((DateTime)b.DataContext);
                        }
                    }
                }
            }
        }
        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                if (_isControlPressed && Owner.SelectionMode == CalendarSelectionMode.MultipleRange)
                {
                    CalendarDayButton b = sender as CalendarDayButton;
                    Debug.Assert(b != null, "The sender should be a non-null CalendarDayButton!");

                    if (b.IsSelected)
                    {
                        Owner.HoverStart = null;
                        _isMouseLeftButtonDown = false;
                        b.IsSelected = false;
                        if (b.DataContext != null)
                        {
                            Owner.SelectedDates.Remove((DateTime)b.DataContext);
                        }
                    }
                }
            }
            _isControlPressed = false;
        }

        private void Month_CalendarButtonMouseDown(object sender, PointerPressedEventArgs e)
        {
            CalendarButton b = sender as CalendarButton;
            Debug.Assert(b != null, "The sender should be a non-null CalendarDayButton!");

            _isMouseLeftButtonDownYearView = true;

            if (e != null)
            {
                _downEventArgYearView = e;
            }

            UpdateYearViewSelection(b);
        }

        internal void Month_CalendarButtonMouseUp(object sender, PointerReleasedEventArgs e)
        {
            _isMouseLeftButtonDownYearView = false;

            if (Owner != null)
            {
                DateTime newmonth = (DateTime)((CalendarButton)sender).DataContext;

                if (Owner.DisplayMode == CalendarMode.Year)
                {
                    Owner.DisplayDate = newmonth;
                    Owner.DisplayMode = CalendarMode.Month;
                }
                else
                {
                    Debug.Assert(Owner.DisplayMode == CalendarMode.Decade, "The owning Calendar should be in decade mode!");
                    Owner.SelectedMonth = newmonth;
                    Owner.DisplayMode = CalendarMode.Year;
                }
            }
        }

        private void Month_MouseEnter(object sender, PointerEventArgs e)
        {
            if (_isMouseLeftButtonDownYearView)
            {
                CalendarButton b = sender as CalendarButton;
                Debug.Assert(b != null, "The sender should be a non-null CalendarDayButton!");
                UpdateYearViewSelection(b);
            }
        }

        private void Month_MouseLeave(object sender, PointerEventArgs e)
        {
            if (_isMouseLeftButtonDownYearView)
            {
                CalendarButton b = sender as CalendarButton;
                // The button is in Pressed state. Change the state to normal.
                if (e.Device.Captured == b)
                    e.Device.Capture(null);
                //b.ReleaseMouseCapture();
                if (_downEventArgYearView != null)
                {
                    var args =
                        new PointerReleasedEventArgs()
                        {
                            Device = _downEventArgYearView.Device,
                            MouseButton = _downEventArgYearView.MouseButton,
                            Handled = _downEventArgYearView.Handled,
                            InputModifiers = _downEventArgYearView.InputModifiers,
                            Route = _downEventArgYearView.Route,
                            Source = _downEventArgYearView.Source
                        };

                    b.SendMouseLeftButtonUp(args);
                }
                _lastCalendarButton = b;
            }
        }
        private void MonthView_MouseLeave(object sender, PointerEventArgs e)
        {
            if (_lastCalendarDayButton != null)
            {
                e.Device.Capture(_lastCalendarDayButton);
            }
        }

        private void YearView_MouseLeave(object sender, PointerEventArgs e)
        {
            if (_lastCalendarButton != null)
            {
                e.Device.Capture(_lastCalendarButton);
            }
        }
        
        internal void UpdateDisabled(bool isEnabled)
        {
            PseudoClasses.Set(":calendardisabled", !isEnabled);
        }
    }
}
