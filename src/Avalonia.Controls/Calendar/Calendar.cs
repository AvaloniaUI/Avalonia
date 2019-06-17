// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies values for the different modes of operation of a
    /// <see cref="T:Avalonia.Controls.Calendar" />.
    /// </summary>
    public enum CalendarMode
    {
        /// <summary>
        /// The <see cref="T:Avalonia.Controls.Calendar" /> displays a
        /// month at a time.
        /// </summary>
        Month = 0,

        /// <summary>
        /// The <see cref="T:Avalonia.Controls.Calendar" /> displays a
        /// year at a time.
        /// </summary>
        Year = 1,

        /// <summary>
        /// The <see cref="T:Avalonia.Controls.Calendar" /> displays a
        /// decade at a time.
        /// </summary>
        Decade = 2,
    }

    /// <summary>
    /// Specifies values that describe the available selection modes for a
    /// <see cref="T:Avalonia.Controls.Calendar" />.
    /// </summary>
    /// <remarks>
    /// This enumeration provides the values that are used by the SelectionMode
    /// property.
    /// </remarks>
    public enum CalendarSelectionMode
    {
        /// <summary>
        /// Only a single date can be selected. Use the
        /// <see cref="P:Avalonia.Controls.Calendar.SelectedDate" />
        /// property to retrieve the selected date.
        /// </summary>
        SingleDate = 0,

        /// <summary>
        /// A single range of dates can be selected. Use 
        /// <see cref="P:Avalonia.Controls.Calendar.SelectedDates" />
        /// property to retrieve the selected dates.
        /// </summary>
        SingleRange = 1,

        /// <summary>
        /// Multiple non-contiguous ranges of dates can be selected. Use the
        /// <see cref="P:Avalonia.Controls.Calendar.SelectedDates" />
        /// property to retrieve the selected dates.
        /// </summary>
        MultipleRange = 2,

        /// <summary>
        /// No selections are allowed.
        /// </summary>
        None = 3,
    }

    /// <summary>
    /// Provides data for the
    /// <see cref="E:Avalonia.Controls.Calendar.DisplayDateChanged" />
    /// event.
    /// </summary>
    public class CalendarDateChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the date that was previously displayed.
        /// </summary>
        /// <value>
        /// The date previously displayed.
        /// </value>
        public DateTime? RemovedDate { get; private set; }

        /// <summary>
        /// Gets the date to be newly displayed.
        /// </summary>
        /// <value>The new date to display.</value>
        public DateTime? AddedDate { get; private set; }

        /// <summary>
        /// Initializes a new instance of the CalendarDateChangedEventArgs
        /// class.
        /// </summary>
        /// <param name="removedDate">
        /// The date that was previously displayed.
        /// </param>
        /// <param name="addedDate">The date to be newly displayed.</param>
        internal CalendarDateChangedEventArgs(DateTime? removedDate, DateTime? addedDate)
        {
            RemovedDate = removedDate;
            AddedDate = addedDate;
        }
    }

    /// <summary>
    /// Provides data for the
    /// <see cref="E:Avalonia.Controls.Calendar.DisplayModeChanged" />
    /// event.
    /// </summary>
    public class CalendarModeChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the previous mode of the
        /// <see cref="T:Avalonia.Controls.Calendar" />.
        /// </summary>
        /// <value>
        /// A <see cref="T:Avalonia.Controls.CalendarMode" /> representing
        /// the previous mode.
        /// </value>
        public CalendarMode OldMode { get; private set; }

        /// <summary>
        /// Gets the new mode of the
        /// <see cref="T:Avalonia.Controls.Calendar" />.
        /// </summary>
        /// <value>
        /// A <see cref="T:Avalonia.Controls.CalendarMode" /> 
        /// the new mode.
        /// </value>
        public CalendarMode NewMode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.CalendarModeChangedEventArgs" />
        /// class.
        /// </summary>
        /// <param name="oldMode">The previous mode.</param>
        /// <param name="newMode">The new mode.</param>
        public CalendarModeChangedEventArgs(CalendarMode oldMode, CalendarMode newMode)
        {
            OldMode = oldMode;
            NewMode = newMode;
        }
    }

    /// <summary>
    /// Represents a control that enables a user to select a date by using a
    /// visual calendar display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Calendar control can be used on its own, or as a drop-down part of a
    /// DatePicker control. For more information, see DatePicker.  A Calendar
    /// displays either the days of a month, the months of a year, or the years
    /// of a decade, depending on the value of the DisplayMode property.  When
    /// displaying the days of a month, the user can select a date, a range of
    /// dates, or multiple ranges of dates.  The kinds of selections that are
    /// allowed are controlled by the SelectionMode property.
    /// </para>
    /// <para>
    /// The range of dates displayed is governed by the DisplayDateStart and
    /// DisplayDateEnd properties.  If DisplayMode is Year or Decade, only
    /// months or years that contain displayable dates will be displayed.
    /// Setting the displayable range to a range that does not include the
    /// current DisplayDate will throw an ArgumentOutOfRangeException.
    /// </para>
    /// <para>
    /// The BlackoutDates property can be used to specify dates that cannot be
    /// selected. These dates will be displayed as dimmed and disabled.
    /// </para>
    /// <para>
    /// By default, Today is highlighted.  This can be disabled by setting
    /// IsTodayHighlighted to false.
    /// </para>
    /// <para>
    /// The Calendar control provides basic navigation using either the mouse or
    /// keyboard. The following table summarizes keyboard navigation.
    /// 
    ///     Key Combination     DisplayMode     Action
    ///     ARROW               Any             Change focused date, unselect
    ///                                         all selected dates, and select
    ///                                         new focused date.
    ///                                         
    ///     SHIFT+ARROW         Any             If SelectionMode is not set to
    ///                                         SingleDate or None begin
    ///                                         selecting a range of dates.
    ///                                         
    ///     CTRL+UP ARROW       Any             Switch to the next larger
    ///                                         DisplayMode.  If DisplayMode is
    ///                                         already Decade, no action.
    ///                                         
    ///     CTRL+DOWN ARROW     Any             Switch to the next smaller
    ///                                         DisplayMode.  If DisplayMode is
    ///                                         already Month, no action.
    ///                                         
    ///     SPACEBAR            Month           Select focused date.
    ///     
    ///     SPACEBAR            Year or Decade  Switch DisplayMode to the Month
    ///                                         or Year represented by focused
    ///                                         item.
    /// </para>
    /// <para>
    /// XAML Usage for Classes Derived from Calendar
    /// If you define a class that derives from Calendar, the class can be used
    /// as an object element in XAML, and all of the inherited properties and
    /// events that show a XAML usage in the reference for the Calendar members
    /// can have the same XAML usage for the derived class. However, the object
    /// element itself must have a different prefix mapping than the controls:
    /// mapping shown in the usages, because the derived class comes from an
    /// assembly and namespace that you create and define.  You must define your
    /// own prefix mapping to an XML namespace to use the class as an object
    /// element in XAML.
    /// </para>
    /// </remarks>
    public class Calendar : TemplatedControl
    {
        internal const int RowsPerMonth = 7;
        internal const int ColumnsPerMonth = 7;
        internal const int RowsPerYear = 3;
        internal const int ColumnsPerYear = 4;

        private DateTime? _selectedDate;
        private DateTime _selectedMonth;
        private DateTime _selectedYear;

        private DateTime _displayDate = DateTime.Today;
        private DateTime? _displayDateStart = null;
        private DateTime? _displayDateEnd = null;

        private bool _isShiftPressed;
        private bool _displayDateIsChanging = false;

        internal CalendarDayButton FocusButton { get; set; }
        internal CalendarButton FocusCalendarButton { get; set; }

        internal Panel Root { get; set; }
        internal CalendarItem MonthControl
        {
            get
            {

                if (Root != null && Root.Children.Count > 0)
                {
                    return Root.Children[0] as CalendarItem;
                }
                return null;
            }
        }

        public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
            AvaloniaProperty.Register<Calendar, DayOfWeek>(
                    nameof(FirstDayOfWeek),
                    defaultValue: DateTimeHelper.GetCurrentDateFormat().FirstDayOfWeek);
        /// <summary>
        /// Gets or sets the day that is considered the beginning of the week.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.DayOfWeek" /> representing the beginning of
        /// the week. The default is <see cref="F:System.DayOfWeek.Sunday" />.
        /// </value>
        public DayOfWeek FirstDayOfWeek
        {
            get { return GetValue(FirstDayOfWeekProperty); }
            set { SetValue(FirstDayOfWeekProperty, value); }
        }
        /// <summary>
        /// FirstDayOfWeekProperty property changed handler.
        /// </summary>
        /// <param name="e">The DependencyPropertyChangedEventArgs.</param>
        private void OnFirstDayOfWeekChanged(AvaloniaPropertyChangedEventArgs e)
        {

            if (IsValidFirstDayOfWeek(e.NewValue))
            {
                UpdateMonths();
            }
            else
            {
                throw new ArgumentOutOfRangeException("d", "Invalid DayOfWeek");
            }
        }
        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        /// <param name="value">Inherited code: Requires comment 1.</param>
        /// <returns>Inherited code: Requires comment 2.</returns>
        private static bool IsValidFirstDayOfWeek(object value)
        {
            DayOfWeek day = (DayOfWeek)value;

            return day == DayOfWeek.Sunday
                || day == DayOfWeek.Monday
                || day == DayOfWeek.Tuesday
                || day == DayOfWeek.Wednesday
                || day == DayOfWeek.Thursday
                || day == DayOfWeek.Friday
                || day == DayOfWeek.Saturday;
        }

        public static readonly StyledProperty<bool> IsTodayHighlightedProperty =
            AvaloniaProperty.Register<Calendar, bool>(
                nameof(IsTodayHighlighted),
                defaultValue: true);
        /// <summary>
        /// Gets or sets a value indicating whether the current date is
        /// highlighted.
        /// </summary>
        /// <value>
        /// True if the current date is highlighted; otherwise, false. The
        /// default is true.
        /// </value>
        public bool IsTodayHighlighted
        {
            get { return GetValue(IsTodayHighlightedProperty); }
            set { SetValue(IsTodayHighlightedProperty, value); }
        }
        /// <summary>
        /// IsTodayHighlightedProperty property changed handler.
        /// </summary>
        /// <param name="e">The DependencyPropertyChangedEventArgs.</param>
        private void OnIsTodayHighlightedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (DisplayDate != null)
            {
                int i = DateTimeHelper.CompareYearMonth(DisplayDateInternal, DateTime.Today);

                if (i > -2 && i < 2)
                {
                    UpdateMonths();
                }
            }
        }

        public static readonly StyledProperty<IBrush> HeaderBackgroundProperty =
            AvaloniaProperty.Register<Calendar, IBrush>(nameof(HeaderBackground));
        public IBrush HeaderBackground
        {
            get { return GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        public static readonly StyledProperty<CalendarMode> DisplayModeProperty =
            AvaloniaProperty.Register<Calendar, CalendarMode>(
                nameof(DisplayMode),
                validate: ValidateDisplayMode);
        /// <summary>
        /// Gets or sets a value indicating whether the calendar is displayed in
        /// months, years, or decades.
        /// </summary>
        /// <value>
        /// A value indicating what length of time the
        /// <see cref="T:System.Windows.Controls.Calendar" /> should display.
        /// </value>
        public CalendarMode DisplayMode
        {
            get { return GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }
        /// <summary>
        /// DisplayModeProperty property changed handler.
        /// </summary>
        /// <param name="e">The DependencyPropertyChangedEventArgs.</param>
        private void OnDisplayModePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            CalendarMode mode = (CalendarMode)e.NewValue;
            CalendarMode oldMode = (CalendarMode)e.OldValue;
            CalendarItem monthControl = MonthControl;

            if (monthControl != null)
            {
                switch (oldMode)
                {
                    case CalendarMode.Month:
                        {
                            SelectedYear = DisplayDateInternal;
                            SelectedMonth = DisplayDateInternal;
                            break;
                        }
                    case CalendarMode.Year:
                        {
                            DisplayDate = SelectedMonth;
                            SelectedYear = SelectedMonth;
                            break;
                        }
                    case CalendarMode.Decade:
                        {
                            DisplayDate = SelectedYear;
                            SelectedMonth = SelectedYear;
                            break;
                        }
                }

                switch (mode)
                {
                    case CalendarMode.Month:
                        {
                            OnMonthClick();
                            break;
                        }
                    case CalendarMode.Year:
                    case CalendarMode.Decade:
                        {
                            OnHeaderClick();
                            break;
                        }
                }
            }
            OnDisplayModeChanged(new CalendarModeChangedEventArgs((CalendarMode)e.OldValue, mode));
        }
        private static CalendarMode ValidateDisplayMode(Calendar o, CalendarMode mode)
        {
            if(IsValidDisplayMode(mode))
            {
                return mode;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(mode), "Invalid DisplayMode");
            }
        }
        private static bool IsValidDisplayMode(CalendarMode mode)
        {
            return mode == CalendarMode.Month
                || mode == CalendarMode.Year
                || mode == CalendarMode.Decade;
        }
        private void OnDisplayModeChanged(CalendarModeChangedEventArgs args)
        {
            DisplayModeChanged?.Invoke(this, args);
        }

        public static readonly StyledProperty<CalendarSelectionMode> SelectionModeProperty =
            AvaloniaProperty.Register<Calendar, CalendarSelectionMode>(
                nameof(SelectionMode),
                defaultValue: CalendarSelectionMode.SingleDate);
        /// <summary>
        /// Gets or sets a value that indicates what kind of selections are
        /// allowed.
        /// </summary>
        /// <value>
        /// A value that indicates the current selection mode. The default is
        /// <see cref="F:System.Windows.Controls.CalendarSelectionMode.SingleDate" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property determines whether the Calendar allows no selection,
        /// selection of a single date, or selection of multiple dates.  The
        /// selection mode is specified with the CalendarSelectionMode
        /// enumeration.
        /// </para>
        /// <para>
        /// When this property is changed, all selected dates will be cleared.
        /// </para>
        /// </remarks>
        public CalendarSelectionMode SelectionMode
        {
            get { return GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }
        private void OnSelectionModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (IsValidSelectionMode(e.NewValue))
            {
                _displayDateIsChanging = true;
                SelectedDate = null;
                _displayDateIsChanging = false;
                SelectedDates.Clear();
            }
            else
            {
                throw new ArgumentOutOfRangeException("d", "Invalid SelectionMode");
            }
        }
        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        /// <param name="value">Inherited code: Requires comment 1.</param>
        /// <returns>Inherited code: Requires comment 2.</returns>
        private static bool IsValidSelectionMode(object value)
        {
            CalendarSelectionMode mode = (CalendarSelectionMode)value;

            return mode == CalendarSelectionMode.SingleDate
                || mode == CalendarSelectionMode.SingleRange
                || mode == CalendarSelectionMode.MultipleRange
                || mode == CalendarSelectionMode.None;
        }

        public static readonly DirectProperty<Calendar, DateTime?> SelectedDateProperty =
            AvaloniaProperty.RegisterDirect<Calendar, DateTime?>(
                nameof(SelectedDate),
                o => o.SelectedDate,
                (o, v) => o.SelectedDate = v,
                defaultBindingMode: BindingMode.TwoWay);
        /// <summary>
        /// Gets or sets the currently selected date.
        /// </summary>
        /// <value>The date currently selected. The default is null.</value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The given date is outside the range specified by
        /// <see cref="P:System.Windows.Controls.Calendar.DisplayDateStart" />
        /// and <see cref="P:System.Windows.Controls.Calendar.DisplayDateEnd" />
        /// -or-
        /// The given date is in the
        /// <see cref="P:System.Windows.Controls.Calendar.BlackoutDates" />
        /// collection.
        /// </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// If set to anything other than null when
        /// <see cref="P:System.Windows.Controls.Calendar.SelectionMode" /> is
        /// set to
        /// <see cref="F:System.Windows.Controls.CalendarSelectionMode.None" />.
        /// </exception>
        /// <remarks>
        /// Use this property when SelectionMode is set to SingleDate.  In other
        /// modes, this property will always be the first date in SelectedDates.
        /// </remarks>
        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set { SetAndRaise(SelectedDateProperty, ref _selectedDate, value); }
        }
        private void OnSelectedDateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_displayDateIsChanging)
            {
                if (SelectionMode != CalendarSelectionMode.None)
                {
                    DateTime? addedDate;

                    addedDate = (DateTime?)e.NewValue;

                    if (IsValidDateSelection(this, addedDate))
                    {
                        if (addedDate == null)
                        {
                            SelectedDates.Clear();
                        }
                        else
                        {
                            if (!(SelectedDates.Count > 0 && SelectedDates[0] == addedDate.Value))
                            {
                                foreach (DateTime item in SelectedDates)
                                {
                                    RemovedItems.Add(item);
                                }
                                SelectedDates.ClearInternal();
                                // the value is added as a range so that the
                                // SelectedDatesChanged event can be thrown with
                                // all the removed items
                                SelectedDates.AddRange(addedDate.Value, addedDate.Value);
                            }
                        }

                        // We update the LastSelectedDate for only the Single
                        // mode.  For the other modes it automatically gets
                        // updated when the HoverEnd is updated.
                        if (SelectionMode == CalendarSelectionMode.SingleDate)
                        {
                            LastSelectedDate = addedDate;
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("d", "SelectedDate value is not valid.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("The SelectedDate property cannot be set when the selection mode is None.");
                }
            }
        }

        /// <summary>
        /// Gets a collection of selected dates.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.Windows.Controls.SelectedDatesCollection" />
        /// object that contains the currently selected dates. The default is an
        /// empty collection.
        /// </value>
        /// <remarks>
        /// Dates can be added to the collection either individually or in a
        /// range using the AddRange method.  Depending on the value of the
        /// SelectionMode property, adding a date or range to the collection may
        /// cause it to be cleared.  The following table lists how
        /// CalendarSelectionMode affects the SelectedDates property.
        /// 
        ///     CalendarSelectionMode   Description
        ///     None                    No selections are allowed.  SelectedDate
        ///                             cannot be set and no values can be added
        ///                             to SelectedDates.
        ///                             
        ///     SingleDate              Only a single date can be selected,
        ///                             either by setting SelectedDate or the
        ///                             first value in SelectedDates.  AddRange
        ///                             cannot be used.
        ///                             
        ///     SingleRange             A single range of dates can be selected.
        ///                             Setting SelectedDate, adding a date
        ///                             individually to SelectedDates, or using
        ///                             AddRange will clear all previous values
        ///                             from SelectedDates.
        ///     MultipleRange           Multiple non-contiguous ranges of dates
        ///                             can be selected. Adding a date
        ///                             individually to SelectedDates or using
        ///                             AddRange will not clear SelectedDates.
        ///                             Setting SelectedDate will still clear
        ///                             SelectedDates, but additional dates or
        ///                             range can then be added.  Adding a range
        ///                             that includes some dates that are
        ///                             already selected or overlaps with
        ///                             another range results in the union of
        ///                             the ranges and does not cause an
        ///                             exception.
        /// </remarks>
        public SelectedDatesCollection SelectedDates { get; private set; }
        private static bool IsSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != e.RemovedItems.Count)
            {
                return true;
            }
            foreach (DateTime addedDate in e.AddedItems)
            {
                if (!e.RemovedItems.Contains(addedDate))
                {
                    return true;
                }
            }
            return false;
        }
        internal void OnSelectedDatesCollectionChanged(SelectionChangedEventArgs e)
        {
            if (IsSelectionChanged(e))
            {
                e.RoutedEvent = SelectingItemsControl.SelectionChangedEvent;
                e.Source = this;
                SelectedDatesChanged?.Invoke(this, e);
            }
        }
        
        internal Collection<DateTime> RemovedItems { get; set; }
        internal DateTime? LastSelectedDateInternal { get; set; }
        internal DateTime? LastSelectedDate
        {
            get { return LastSelectedDateInternal; }
            set
            {
                LastSelectedDateInternal = value;

                if (SelectionMode == CalendarSelectionMode.None)
                {
                    if (FocusButton != null)
                    {
                        FocusButton.IsCurrent = false;
                    }
                    FocusButton = FindDayButtonFromDay(LastSelectedDate.Value);
                    if (FocusButton != null)
                    {
                        FocusButton.IsCurrent = HasFocusInternal;
                    }
                }
            }
        }

        internal DateTime SelectedMonth
        {
            get { return _selectedMonth; }
            set
            {
                int monthDifferenceStart = DateTimeHelper.CompareYearMonth(value, DisplayDateRangeStart);
                int monthDifferenceEnd = DateTimeHelper.CompareYearMonth(value, DisplayDateRangeEnd);

                if (monthDifferenceStart >= 0 && monthDifferenceEnd <= 0)
                {
                    _selectedMonth = DateTimeHelper.DiscardDayTime(value);
                }
                else
                {
                    if (monthDifferenceStart < 0)
                    {
                        _selectedMonth = DateTimeHelper.DiscardDayTime(DisplayDateRangeStart);
                    }
                    else
                    {
                        Debug.Assert(monthDifferenceEnd > 0, "monthDifferenceEnd should be greater than 0!");
                        _selectedMonth = DateTimeHelper.DiscardDayTime(DisplayDateRangeEnd);
                    }
                }
            }
        }
        internal DateTime SelectedYear
        {
            get { return _selectedYear; }
            set
            {
                if (value.Year < DisplayDateRangeStart.Year)
                {
                    _selectedYear = DisplayDateRangeStart;
                }
                else
                {
                    if (value.Year > DisplayDateRangeEnd.Year)
                    {
                        _selectedYear = DisplayDateRangeEnd;
                    }
                    else
                    {
                        _selectedYear = value;
                    }
                }
            }
        }

        public static readonly DirectProperty<Calendar, DateTime> DisplayDateProperty =
            AvaloniaProperty.RegisterDirect<Calendar, DateTime>(
                nameof(DisplayDate),
                o => o.DisplayDate,
                (o, v) => o.DisplayDate = v,
                defaultBindingMode: BindingMode.TwoWay);
        /// <summary>
        /// Gets or sets the date to display.
        /// </summary>
        /// <value>The date to display.</value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The given date is not in the range specified by
        /// <see cref="P:System.Windows.Controls.Calendar.DisplayDateStart" />
        /// and
        /// <see cref="P:System.Windows.Controls.Calendar.DisplayDateEnd" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This property allows the developer to specify a date to display.  If
        /// this property is a null reference (Nothing in Visual Basic),
        /// SelectedDate is displayed.  If SelectedDate is also a null reference
        /// (Nothing in Visual Basic), Today is displayed.  The default is
        /// Today.
        /// </para>
        /// <para>
        /// To set this property in XAML, use a date specified in the format
        /// yyyy/mm/dd.  The mm and dd components must always consist of two
        /// characters, with a leading zero if necessary.  For instance, the
        /// month of May should be specified as 05.
        /// </para>
        /// </remarks>
        public DateTime DisplayDate
        {
            get { return _displayDate; }
            set { SetAndRaise(DisplayDateProperty, ref _displayDate, value); }
        }
        internal DateTime DisplayDateInternal { get; private set; }

        private void OnDisplayDateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateDisplayDate(this, (DateTime)e.NewValue, (DateTime)e.OldValue);
        }
        private static void UpdateDisplayDate(Calendar c, DateTime addedDate, DateTime removedDate)
        {
            Contract.Requires<ArgumentNullException>(c != null);

            // If DisplayDate < DisplayDateStart, DisplayDate = DisplayDateStart
            if (DateTime.Compare(addedDate, c.DisplayDateRangeStart) < 0)
            {
                c.DisplayDate = c.DisplayDateRangeStart;
                return;
            }

            // If DisplayDate > DisplayDateEnd, DisplayDate = DisplayDateEnd
            if (DateTime.Compare(addedDate, c.DisplayDateRangeEnd) > 0)
            {
                c.DisplayDate = c.DisplayDateRangeEnd;
                return;
            }

            c.DisplayDateInternal = DateTimeHelper.DiscardDayTime(addedDate);
            c.UpdateMonths();
            c.OnDisplayDate(new CalendarDateChangedEventArgs(removedDate, addedDate));
        }
        private void OnDisplayDate(CalendarDateChangedEventArgs e)
        {
            DisplayDateChanged?.Invoke(this, e);
        }

        public static readonly DirectProperty<Calendar, DateTime?> DisplayDateStartProperty =
            AvaloniaProperty.RegisterDirect<Calendar, DateTime?>(
                nameof(DisplayDateStart),
                o => o.DisplayDateStart,
                (o, v) => o.DisplayDateStart = v,
                defaultBindingMode: BindingMode.TwoWay);
        /// <summary>
        /// Gets or sets the first date to be displayed.
        /// </summary>
        /// <value>The first date to display.</value>
        /// <remarks>
        /// To set this property in XAML, use a date specified in the format
        /// yyyy/mm/dd.  The mm and dd components must always consist of two
        /// characters, with a leading zero if necessary.  For instance, the
        /// month of May should be specified as 05.
        /// </remarks>
        public DateTime? DisplayDateStart
        {
            get { return _displayDateStart; }
            set { SetAndRaise(DisplayDateStartProperty, ref _displayDateStart, value); }
        }
        private void OnDisplayDateStartChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_displayDateIsChanging)
            {
                DateTime? newValue = e.NewValue as DateTime?;

                if (newValue.HasValue)
                {
                    // DisplayDateStart coerces to the value of the
                    // SelectedDateMin if SelectedDateMin < DisplayDateStart
                    DateTime? selectedDateMin = SelectedDateMin(this);

                    if (selectedDateMin.HasValue && DateTime.Compare(selectedDateMin.Value, newValue.Value) < 0)
                    {
                        DisplayDateStart = selectedDateMin.Value;
                        return;
                    }

                    // if DisplayDateStart > DisplayDateEnd,
                    // DisplayDateEnd = DisplayDateStart
                    if (DateTime.Compare(newValue.Value, DisplayDateRangeEnd) > 0)
                    {
                        DisplayDateEnd = DisplayDateStart;
                    }

                    // If DisplayDate < DisplayDateStart,
                    // DisplayDate = DisplayDateStart
                    if (DateTimeHelper.CompareYearMonth(newValue.Value, DisplayDateInternal) > 0)
                    {
                        DisplayDate = newValue.Value;
                    }
                }
                UpdateMonths();
            }
        }

        /// <summary>
        /// Gets a collection of dates that are marked as not selectable.
        /// </summary>
        /// <value>
        /// A collection of dates that cannot be selected. The default value is
        /// an empty collection.
        /// </value>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Adding a date to this collection when it is already selected or
        /// adding a date outside the range specified by DisplayDateStart and
        /// DisplayDateEnd.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Dates in this collection will appear as disabled on the calendar.
        /// </para>
        /// <para>
        /// To make all past dates not selectable, you can use the
        /// AddDatesInPast method provided by the collection returned by this
        /// property.
        /// </para>
        /// </remarks>
        public CalendarBlackoutDatesCollection BlackoutDates { get; private set; }

        private static DateTime? SelectedDateMin(Calendar cal)
        {
            DateTime selectedDateMin;

            if (cal.SelectedDates.Count > 0)
            {
                selectedDateMin = cal.SelectedDates[0];
                Debug.Assert(DateTime.Compare(cal.SelectedDate.Value, selectedDateMin) == 0, "The SelectedDate should be the minimum selected date!");
            }
            else
            {
                return null;
            }

            foreach (DateTime selectedDate in cal.SelectedDates)
            {
                if (DateTime.Compare(selectedDate, selectedDateMin) < 0)
                {
                    selectedDateMin = selectedDate;
                }
            }
            return selectedDateMin;
        }
        internal DateTime DisplayDateRangeStart
        {
            get { return DisplayDateStart.GetValueOrDefault(DateTime.MinValue); }
        }

        public static readonly DirectProperty<Calendar, DateTime?> DisplayDateEndProperty =
            AvaloniaProperty.RegisterDirect<Calendar, DateTime?>(
                nameof(DisplayDateEnd),
                o => o.DisplayDateEnd,
                (o, v) => o.DisplayDateEnd = v,
                defaultBindingMode: BindingMode.TwoWay);
        
        /// <summary>
        /// Gets or sets the last date to be displayed.
        /// </summary>
        /// <value>The last date to display.</value>
        /// <remarks>
        /// To set this property in XAML, use a date specified in the format
        /// yyyy/mm/dd.  The mm and dd components must always consist of two
        /// characters, with a leading zero if necessary.  For instance, the
        /// month of May should be specified as 05.
        /// </remarks>
        public DateTime? DisplayDateEnd
        {
            get { return _displayDateEnd; }
            set { SetAndRaise(DisplayDateEndProperty, ref _displayDateEnd, value); }
        }

        private void OnDisplayDateEndChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_displayDateIsChanging)
            {
                DateTime? newValue = e.NewValue as DateTime?;

                if (newValue.HasValue)
                {
                    // DisplayDateEnd coerces to the value of the
                    // SelectedDateMax if SelectedDateMax > DisplayDateEnd
                    DateTime? selectedDateMax = SelectedDateMax(this);

                    if (selectedDateMax.HasValue && DateTime.Compare(selectedDateMax.Value, newValue.Value) > 0)
                    {
                        DisplayDateEnd = selectedDateMax.Value;
                        return;
                    }

                    // if DisplayDateEnd < DisplayDateStart,
                    // DisplayDateEnd = DisplayDateStart
                    if (DateTime.Compare(newValue.Value, DisplayDateRangeStart) < 0)
                    {
                        DisplayDateEnd = DisplayDateStart;
                        return;
                    }

                    // If DisplayDate > DisplayDateEnd,
                    // DisplayDate = DisplayDateEnd
                    if (DateTimeHelper.CompareYearMonth(newValue.Value, DisplayDateInternal) < 0)
                    {
                        DisplayDate = newValue.Value;
                    }
                }
                UpdateMonths();
            }
        }

        private static DateTime? SelectedDateMax(Calendar cal)
        {
            DateTime selectedDateMax;

            if (cal.SelectedDates.Count > 0)
            {
                selectedDateMax = cal.SelectedDates[0];
                Debug.Assert(DateTime.Compare(cal.SelectedDate.Value, selectedDateMax) == 0, "The SelectedDate should be the maximum SelectedDate!");
            }
            else
            {
                return null;
            }

            foreach (DateTime selectedDate in cal.SelectedDates)
            {
                if (DateTime.Compare(selectedDate, selectedDateMax) > 0)
                {
                    selectedDateMax = selectedDate;
                }
            }
            return selectedDateMax;
        }
        internal DateTime DisplayDateRangeEnd
        {
            get { return DisplayDateEnd.GetValueOrDefault(DateTime.MaxValue); }
        }

        internal DateTime? HoverStart { get; set; }
        internal int? HoverStartIndex { get; set; }
        internal DateTime? HoverEndInternal { get; set; }
        internal DateTime? HoverEnd
        {
            get { return HoverEndInternal; }
            set
            {
                HoverEndInternal = value;
                LastSelectedDate = value;
            }
        }
        internal int? HoverEndIndex { get; set; }
        internal bool HasFocusInternal { get; set; }
        internal bool IsMouseSelection { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether DatePicker should change its 
        /// DisplayDate because of a SelectedDate change on its Calendar.
        /// </summary>
        internal bool DatePickerDisplayDateFlag { get; set; }

        internal CalendarDayButton FindDayButtonFromDay(DateTime day)
        {
            CalendarItem monthControl = MonthControl;

            // REMOVE_RTM: should be updated if we support MultiCalendar
            int count = RowsPerMonth * ColumnsPerMonth;
            if (monthControl != null)
            {
                if (monthControl.MonthView != null)
                {
                    for (int childIndex = ColumnsPerMonth; childIndex < count; childIndex++)
                    {
                        if (monthControl.MonthView.Children[childIndex] is CalendarDayButton b)
                        {
                            var d = b.DataContext as DateTime?;

                            if (d.HasValue)
                            {
                                if (DateTimeHelper.CompareDays(d.Value, day) == 0)
                                {
                                    return b;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void OnSelectedMonthChanged(DateTime? selectedMonth)
        {
            if (selectedMonth.HasValue)
            {
                Debug.Assert(DisplayMode == CalendarMode.Year, "DisplayMode should be Year!");
                SelectedMonth = selectedMonth.Value;
                UpdateMonths();
            }
        }
        private void OnSelectedYearChanged(DateTime? selectedYear)
        {
            if (selectedYear.HasValue)
            {
                Debug.Assert(DisplayMode == CalendarMode.Decade, "DisplayMode should be Decade!");
                SelectedYear = selectedYear.Value;
                UpdateMonths();
            }
        }
        internal void OnHeaderClick()
        {
            Debug.Assert(DisplayMode == CalendarMode.Year || DisplayMode == CalendarMode.Decade, "The DisplayMode should be Year or Decade");
            CalendarItem monthControl = MonthControl;
            if (monthControl != null && monthControl.MonthView != null && monthControl.YearView != null)
            {
                monthControl.MonthView.IsVisible = false;
                monthControl.YearView.IsVisible = true;
                UpdateMonths();
            }
        }

        internal void ResetStates()
        {
            CalendarItem monthControl = MonthControl;
            int count = RowsPerMonth * ColumnsPerMonth;
            if (monthControl != null)
            {
                if (monthControl.MonthView != null)
                {
                    for (int childIndex = ColumnsPerMonth; childIndex < count; childIndex++)
                    {
                        var d = (CalendarDayButton)monthControl.MonthView.Children[childIndex];
                        d.IgnoreMouseOverState();
                    }
                }
            }

        }

        internal void UpdateMonths()
        {
            CalendarItem monthControl = MonthControl;
            if (monthControl != null)
            {
                switch (DisplayMode)
                {
                    case CalendarMode.Month:
                        {
                            monthControl.UpdateMonthMode();
                            break;
                        }
                    case CalendarMode.Year:
                        {
                            monthControl.UpdateYearMode();
                            break;
                        }
                    case CalendarMode.Decade:
                        {
                            monthControl.UpdateDecadeMode();
                            break;
                        }
                }
            }
        }

        internal static bool IsValidDateSelection(Calendar cal, DateTime? value)
        {
            if (!value.HasValue)
            {
                return true;
            }
            else
            {
                if (cal.BlackoutDates.Contains(value.Value))
                {
                    return false;
                }
                else
                {
                    cal._displayDateIsChanging = true;
                    if (DateTime.Compare(value.Value, cal.DisplayDateRangeStart) < 0)
                    {
                        cal.DisplayDateStart = value;
                    }
                    else if (DateTime.Compare(value.Value, cal.DisplayDateRangeEnd) > 0)
                    {
                        cal.DisplayDateEnd = value;
                    }
                    cal._displayDateIsChanging = false;

                    return true;
                }
            }
        }
        private static bool IsValidKeyboardSelection(Calendar cal, DateTime? value)
        {
            if (!value.HasValue)
            {
                return true;
            }
            else
            {
                if (cal.BlackoutDates.Contains(value.Value))
                {
                    return false;
                }
                else
                {
                    return (DateTime.Compare(value.Value, cal.DisplayDateRangeStart) >= 0 && DateTime.Compare(value.Value, cal.DisplayDateRangeEnd) <= 0);
                }
            }
        }

        /// <summary>
        /// This method highlights the days in MultiSelection mode without
        /// adding them to the SelectedDates collection.
        /// </summary>
        internal void HighlightDays()
        {
            if (HoverEnd != null && HoverStart != null)
            {
                int startIndex, endIndex, i;
                CalendarItem monthControl = MonthControl;

                // This assumes a contiguous set of dates:
                if (HoverEndIndex != null && HoverStartIndex != null)
                {
                    SortHoverIndexes(out startIndex, out endIndex);

                    for (i = startIndex; i <= endIndex; i++)
                    {
                        if (monthControl.MonthView.Children[i] is CalendarDayButton b)
                        {
                            b.IsSelected = true;
                            var d = b.DataContext as DateTime?;

                            if (d.HasValue && DateTimeHelper.CompareDays(HoverEnd.Value, d.Value) == 0)
                            {
                                if (FocusButton != null)
                                {
                                    FocusButton.IsCurrent = false;
                                }
                                b.IsCurrent = HasFocusInternal;
                                FocusButton = b;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// This method un-highlights the days that were hovered over but not
        /// added to the SelectedDates collection or un-highlighted the
        /// previously selected days in SingleRange Mode.
        /// </summary>
        internal void UnHighlightDays()
        {
            if (HoverEnd != null && HoverStart != null)
            {
                CalendarItem monthControl = MonthControl;

                if (HoverEndIndex != null && HoverStartIndex != null)
                {
                    int i;
                    SortHoverIndexes(out int startIndex, out int endIndex);

                    if (SelectionMode == CalendarSelectionMode.MultipleRange)
                    {
                        for (i = startIndex; i <= endIndex; i++)
                        {
                            if (monthControl.MonthView.Children[i] is CalendarDayButton b)
                            {
                                var d = b.DataContext as DateTime?;

                                if (d.HasValue)
                                {
                                    if (!SelectedDates.Contains(d.Value))
                                    {
                                        b.IsSelected = false;
                                    }
                                } 
                            }
                        }
                    }
                    else
                    {
                        // It is SingleRange
                        for (i = startIndex; i <= endIndex; i++)
                        {
                            ((CalendarDayButton)monthControl.MonthView.Children[i]).IsSelected = false;
                        }
                    }
                }
            }
        }
        internal void SortHoverIndexes(out int startIndex, out int endIndex)
        {
            if (DateTimeHelper.CompareDays(HoverEnd.Value, HoverStart.Value) > 0)
            {
                startIndex = HoverStartIndex.Value;
                endIndex = HoverEndIndex.Value;
            }
            else
            {
                startIndex = HoverEndIndex.Value;
                endIndex = HoverStartIndex.Value;
            }
        }

        internal void OnPreviousClick()
        {
            if (DisplayMode == CalendarMode.Month && DisplayDate != null)
            {
                DateTime? d = DateTimeHelper.AddMonths(DateTimeHelper.DiscardDayTime(DisplayDate), -1);
                if (d.HasValue)
                {
                    if (!LastSelectedDate.HasValue || DateTimeHelper.CompareYearMonth(LastSelectedDate.Value, d.Value) != 0)
                    {
                        LastSelectedDate = d.Value;
                    }
                    DisplayDate = d.Value;
                }
            }
            else
            {
                if (DisplayMode == CalendarMode.Year)
                {
                    DateTime? d = DateTimeHelper.AddYears(new DateTime(SelectedMonth.Year, 1, 1), -1);

                    if (d.HasValue)
                    {
                        SelectedMonth = d.Value;
                    }
                    else
                    {
                        SelectedMonth = DateTimeHelper.DiscardDayTime(DisplayDateRangeStart);
                    }
                }
                else
                {
                    Debug.Assert(DisplayMode == CalendarMode.Decade, "DisplayMode should be Decade!");

                    DateTime? d = DateTimeHelper.AddYears(new DateTime(SelectedYear.Year, 1, 1), -10);

                    if (d.HasValue)
                    {
                        int decade = Math.Max(1, DateTimeHelper.DecadeOfDate(d.Value));
                        SelectedYear = new DateTime(decade, 1, 1);
                    }
                    else
                    {
                        SelectedYear = DateTimeHelper.DiscardDayTime(DisplayDateRangeStart);
                    }
                }
                UpdateMonths();
            }
        }
        internal void OnNextClick()
        {
            if (DisplayMode == CalendarMode.Month && DisplayDate != null)
            {
                DateTime? d = DateTimeHelper.AddMonths(DateTimeHelper.DiscardDayTime(DisplayDate), 1);
                if (d.HasValue)
                {
                    if (!LastSelectedDate.HasValue || DateTimeHelper.CompareYearMonth(LastSelectedDate.Value, d.Value) != 0)
                    {
                        LastSelectedDate = d.Value;
                    }
                    DisplayDate = d.Value;
                }
            }
            else
            {
                if (DisplayMode == CalendarMode.Year)
                {
                    DateTime? d = DateTimeHelper.AddYears(new DateTime(SelectedMonth.Year, 1, 1), 1);

                    if (d.HasValue)
                    {
                        SelectedMonth = d.Value;
                    }
                    else
                    {
                        SelectedMonth = DateTimeHelper.DiscardDayTime(DisplayDateRangeEnd);
                    }
                }
                else
                {
                    Debug.Assert(DisplayMode == CalendarMode.Decade, "DisplayMode should be Decade");

                    DateTime? d = DateTimeHelper.AddYears(new DateTime(SelectedYear.Year, 1, 1), 10);

                    if (d.HasValue)
                    {
                        int decade = Math.Max(1, DateTimeHelper.DecadeOfDate(d.Value));
                        SelectedYear = new DateTime(decade, 1, 1);
                    }
                    else
                    {
                        SelectedYear = DateTimeHelper.DiscardDayTime(DisplayDateRangeEnd);
                    }
                }
                UpdateMonths();
            }
        }

        /// <summary>
        /// If the day is a trailing day, Update the DisplayDate.
        /// </summary>
        /// <param name="selectedDate">Inherited code: Requires comment.</param>
        internal void OnDayClick(DateTime selectedDate)
        {
            Debug.Assert(DisplayMode == CalendarMode.Month, "DisplayMode should be Month!");
            int i = DateTimeHelper.CompareYearMonth(selectedDate, DisplayDateInternal);

            if (SelectionMode == CalendarSelectionMode.None)
            {
                LastSelectedDate = selectedDate;
            }

            if (i > 0)
            {
                OnNextClick();
            }
            else if (i < 0)
            {
                OnPreviousClick();
            }
        }
        private void OnMonthClick()
        {
            CalendarItem monthControl = MonthControl;
            if (monthControl != null && monthControl.YearView != null && monthControl.MonthView != null)
            {
                monthControl.YearView.IsVisible = false;
                monthControl.MonthView.IsVisible = true;

                if (!LastSelectedDate.HasValue || DateTimeHelper.CompareYearMonth(LastSelectedDate.Value, DisplayDate) != 0)
                {
                    LastSelectedDate = DisplayDate;
                }

                UpdateMonths();
            }
        }

        public override string ToString()
        {
            if (SelectedDate != null)
            {
                return SelectedDate.Value.ToString(DateTimeHelper.GetCurrentDateFormat());
            }
            else
            {
                return string.Empty;
            }
        }

        public event EventHandler<SelectionChangedEventArgs> SelectedDatesChanged;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:System.Windows.Controls.Calendar.DisplayDate" />
        /// property is changed.
        /// </summary>
        /// <remarks>
        /// This event occurs after DisplayDate is assigned its new value.
        /// </remarks>
        public event EventHandler<CalendarDateChangedEventArgs> DisplayDateChanged;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:System.Windows.Controls.Calendar.DisplayMode" />
        /// property is changed.
        /// </summary>
        public event EventHandler<CalendarModeChangedEventArgs> DisplayModeChanged;

        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        internal event EventHandler<PointerReleasedEventArgs> DayButtonMouseUp;

        /// <summary>
        /// This method adds the days that were selected by Keyboard to the
        /// SelectedDays Collection.
        /// </summary>
        private void AddSelection()
        {
            if (HoverEnd != null && HoverStart != null)
            {
                foreach (DateTime item in SelectedDates)
                {
                    RemovedItems.Add(item);
                }

                SelectedDates.ClearInternal();
                // In keyboard selection, we are sure that the collection does
                // not include any blackout days
                SelectedDates.AddRange(HoverStart.Value, HoverEnd.Value);
            }
        }
        private void ProcessSelection(bool shift, DateTime? lastSelectedDate, int? index)
        {
            if (SelectionMode == CalendarSelectionMode.None && lastSelectedDate != null)
            {
                OnDayClick(lastSelectedDate.Value);
                return;
            }
            if (lastSelectedDate != null && IsValidKeyboardSelection(this, lastSelectedDate.Value))
            {
                if (SelectionMode == CalendarSelectionMode.SingleRange || SelectionMode == CalendarSelectionMode.MultipleRange)
                {
                    foreach (DateTime item in SelectedDates)
                    {
                        RemovedItems.Add(item);
                    }
                    SelectedDates.ClearInternal();
                    if (shift)
                    {
                        CalendarDayButton b;
                        _isShiftPressed = true;
                        if (HoverStart == null)
                        {
                            if (LastSelectedDate != null)
                            {
                                HoverStart = LastSelectedDate;
                            }
                            else
                            {
                                if (DateTimeHelper.CompareYearMonth(DisplayDateInternal, DateTime.Today) == 0)
                                {
                                    HoverStart = DateTime.Today;
                                }
                                else
                                {
                                    HoverStart = DisplayDateInternal;
                                }
                            }

                            b = FindDayButtonFromDay(HoverStart.Value);
                            if (b != null)
                            {
                                HoverStartIndex = b.Index;
                            }
                        }
                        // the index of the SelectedDate is always the last
                        // selectedDate's index
                        UnHighlightDays();
                        // If we hit a BlackOutDay with keyboard we do not
                        // update the HoverEnd
                        CalendarDateRange range;

                        if (DateTime.Compare(HoverStart.Value, lastSelectedDate.Value) < 0)
                        {
                            range = new CalendarDateRange(HoverStart.Value, lastSelectedDate.Value);
                        }
                        else
                        {
                            range = new CalendarDateRange(lastSelectedDate.Value, HoverStart.Value);
                        }

                        if (!BlackoutDates.ContainsAny(range))
                        {
                            HoverEnd = lastSelectedDate;

                            if (index.HasValue)
                            {
                                HoverEndIndex += index;
                            }
                            else
                            {
                                // For Home, End, PageUp and PageDown Keys there
                                // is no easy way to predict the index value
                                b = FindDayButtonFromDay(HoverEndInternal.Value);

                                if (b != null)
                                {
                                    HoverEndIndex = b.Index;
                                }
                            }
                        }

                        OnDayClick(HoverEnd.Value);
                        HighlightDays();
                    }
                    else
                    {
                        HoverStart = lastSelectedDate;
                        HoverEnd = lastSelectedDate;
                        AddSelection();
                        OnDayClick(lastSelectedDate.Value);
                    }
                }
                else
                {
                    // ON CLEAR 
                    LastSelectedDate = lastSelectedDate.Value;
                    if (SelectedDates.Count > 0)
                    {
                        SelectedDates[0] = lastSelectedDate.Value;
                    }
                    else
                    {
                        SelectedDates.Add(lastSelectedDate.Value);
                    }
                    OnDayClick(lastSelectedDate.Value);
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (!HasFocusInternal && e.MouseButton == MouseButton.Left)
            {
                this.GetFocusManager().Focus(this);
            }
        }

        internal void OnDayButtonMouseUp(PointerReleasedEventArgs e)
        {
            DayButtonMouseUp?.Invoke(this, e);
        }

        /// <summary>
        /// Default mouse wheel handler for the calendar control.
        /// </summary>
        /// <param name="e">Mouse wheel event args.</param>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (!e.Handled)
            {
                CalendarExtensions.GetMetaKeyState(e.InputModifiers, out bool ctrl, out bool shift);

                if (!ctrl)
                {
                    if (e.Delta.Y > 0)
                    {
                        ProcessPageUpKey(false);
                    }
                    else
                    {
                        ProcessPageDownKey(false);
                    }
                }
                else
                {
                    if (e.Delta.Y > 0)
                    {
                        ProcessDownKey(ctrl, shift);
                    }
                    else
                    {
                        ProcessUpKey(ctrl, shift);
                    }
                }
                e.Handled = true;
            }
        }

        internal void Calendar_KeyDown(KeyEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                e.Handled = ProcessCalendarKey(e);
            }
        }

        internal bool ProcessCalendarKey(KeyEventArgs e)
        {
            if (DisplayMode == CalendarMode.Month)
            {
                if (LastSelectedDate.HasValue && DisplayDateInternal != null)
                {
                    // If a blackout day is inactive, when clicked on it, the
                    // previous inactive day which is not a blackout day can get
                    // the focus.  In this case we should allow keyboard
                    // functions on that inactive day
                    if (DateTimeHelper.CompareYearMonth(LastSelectedDate.Value, DisplayDateInternal) != 0 && FocusButton != null && !FocusButton.IsInactive)
                    {
                        return true;
                    }
                }
            }

            // Some keys (e.g. Left/Right) need to be translated in RightToLeft mode
            Key invariantKey = e.Key;  //InteractionHelper.GetLogicalKey(FlowDirection, e.Key);

            CalendarExtensions.GetMetaKeyState(e.Modifiers, out bool ctrl, out bool shift);

            switch (invariantKey)
            {
                case Key.Up:
                    {
                        ProcessUpKey(ctrl, shift);
                        return true;
                    }
                case Key.Down:
                    {
                        ProcessDownKey(ctrl, shift);
                        return true;
                    }
                case Key.Left:
                    {
                        ProcessLeftKey(shift);
                        return true;
                    }
                case Key.Right:
                    {
                        ProcessRightKey(shift);
                        return true;
                    }
                case Key.PageDown:
                    {
                        ProcessPageDownKey(shift);
                        return true;
                    }
                case Key.PageUp:
                    {
                        ProcessPageUpKey(shift);
                        return true;
                    }
                case Key.Home:
                    {
                        ProcessHomeKey(shift);
                        return true;
                    }
                case Key.End:
                    {
                        ProcessEndKey(shift);
                        return true;
                    }
                case Key.Enter:
                case Key.Space:
                    {
                        return ProcessEnterKey();
                    }
            }
            return false;
        }
        internal void ProcessUpKey(bool ctrl, bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        if (ctrl)
                        {
                            SelectedMonth = DisplayDateInternal;
                            DisplayMode = CalendarMode.Year;
                        }
                        else
                        {
                            DateTime? selectedDate = DateTimeHelper.AddDays(LastSelectedDate.GetValueOrDefault(DateTime.Today), -ColumnsPerMonth);
                            ProcessSelection(shift, selectedDate, -ColumnsPerMonth);
                        }
                        break;
                    }
                case CalendarMode.Year:
                    {
                        if (ctrl)
                        {
                            SelectedYear = SelectedMonth;
                            DisplayMode = CalendarMode.Decade;
                        }
                        else
                        {
                            DateTime? selectedMonth = DateTimeHelper.AddMonths(_selectedMonth, -ColumnsPerYear);
                            OnSelectedMonthChanged(selectedMonth);
                        }
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        if (!ctrl)
                        {
                            DateTime? selectedYear = DateTimeHelper.AddYears(SelectedYear, -ColumnsPerYear);
                            OnSelectedYearChanged(selectedYear);
                        }
                        break;
                    }
            }
        }
        internal void ProcessDownKey(bool ctrl, bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        if (!ctrl || shift)
                        {
                            DateTime? selectedDate = DateTimeHelper.AddDays(LastSelectedDate.GetValueOrDefault(DateTime.Today), ColumnsPerMonth);
                            ProcessSelection(shift, selectedDate, ColumnsPerMonth);
                        }
                        break;
                    }
                case CalendarMode.Year:
                    {
                        if (ctrl)
                        {
                            DisplayDate = SelectedMonth;
                            DisplayMode = CalendarMode.Month;
                        }
                        else
                        {
                            DateTime? selectedMonth = DateTimeHelper.AddMonths(_selectedMonth, ColumnsPerYear);
                            OnSelectedMonthChanged(selectedMonth);
                        }
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        if (ctrl)
                        {
                            SelectedMonth = SelectedYear;
                            DisplayMode = CalendarMode.Year;
                        }
                        else
                        {
                            DateTime? selectedYear = DateTimeHelper.AddYears(SelectedYear, ColumnsPerYear);
                            OnSelectedYearChanged(selectedYear);
                        }
                        break;
                    }
            }
        }
        internal void ProcessLeftKey(bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        DateTime? selectedDate = DateTimeHelper.AddDays(LastSelectedDate.GetValueOrDefault(DateTime.Today), -1);
                        ProcessSelection(shift, selectedDate, -1);
                        break;
                    }
                case CalendarMode.Year:
                    {
                        DateTime? selectedMonth = DateTimeHelper.AddMonths(_selectedMonth, -1);
                        OnSelectedMonthChanged(selectedMonth);
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        DateTime? selectedYear = DateTimeHelper.AddYears(SelectedYear, -1);
                        OnSelectedYearChanged(selectedYear);
                        break;
                    }
            }
        }
        internal void ProcessRightKey(bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        DateTime? selectedDate = DateTimeHelper.AddDays(LastSelectedDate.GetValueOrDefault(DateTime.Today), 1);
                        ProcessSelection(shift, selectedDate, 1);
                        break;
                    }
                case CalendarMode.Year:
                    {
                        DateTime? selectedMonth = DateTimeHelper.AddMonths(_selectedMonth, 1);
                        OnSelectedMonthChanged(selectedMonth);
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        DateTime? selectedYear = DateTimeHelper.AddYears(SelectedYear, 1);
                        OnSelectedYearChanged(selectedYear);
                        break;
                    }
            }
        }
        private bool ProcessEnterKey()
        {
            switch (DisplayMode)
            {
                case CalendarMode.Year:
                    {
                        DisplayDate = SelectedMonth;
                        DisplayMode = CalendarMode.Month;
                        return true;
                    }
                case CalendarMode.Decade:
                    {
                        SelectedMonth = SelectedYear;
                        DisplayMode = CalendarMode.Year;
                        return true;
                    }
            }
            return false;
        }
        internal void ProcessHomeKey(bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        // REMOVE_RTM: Not all types of calendars start with Day1. If Non-Gregorian is supported check this:
                        DateTime? selectedDate = new DateTime(DisplayDateInternal.Year, DisplayDateInternal.Month, 1);
                        ProcessSelection(shift, selectedDate, null);
                        break;
                    }
                case CalendarMode.Year:
                    {
                        DateTime selectedMonth = new DateTime(_selectedMonth.Year, 1, 1);
                        OnSelectedMonthChanged(selectedMonth);
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        DateTime? selectedYear = new DateTime(DateTimeHelper.DecadeOfDate(SelectedYear), 1, 1);
                        OnSelectedYearChanged(selectedYear);
                        break;
                    }
            }
        }
        internal void ProcessEndKey(bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        if (DisplayDate != null)
                        {
                            DateTime? selectedDate = new DateTime(DisplayDateInternal.Year, DisplayDateInternal.Month, 1);

                            if (DateTimeHelper.CompareYearMonth(DateTime.MaxValue, selectedDate.Value) > 0)
                            {
                                // since DisplayDate is not equal to
                                // DateTime.MaxValue we are sure selectedDate is\
                                // not null
                                selectedDate = DateTimeHelper.AddMonths(selectedDate.Value, 1).Value;
                                selectedDate = DateTimeHelper.AddDays(selectedDate.Value, -1).Value;
                            }
                            else
                            {
                                selectedDate = DateTime.MaxValue;
                            }
                            ProcessSelection(shift, selectedDate, null);
                        }
                        break;
                    }
                case CalendarMode.Year:
                    {
                        DateTime selectedMonth = new DateTime(_selectedMonth.Year, 12, 1);
                        OnSelectedMonthChanged(selectedMonth);
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        DateTime? selectedYear = new DateTime(DateTimeHelper.EndOfDecade(SelectedYear), 1, 1);
                        OnSelectedYearChanged(selectedYear);
                        break;
                    }
            }
        }
        internal void ProcessPageDownKey(bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        DateTime? selectedDate = DateTimeHelper.AddMonths(LastSelectedDate.GetValueOrDefault(DateTime.Today), 1);
                        ProcessSelection(shift, selectedDate, null);
                        break;
                    }
                case CalendarMode.Year:
                    {
                        DateTime? selectedMonth = DateTimeHelper.AddYears(_selectedMonth, 1);
                        OnSelectedMonthChanged(selectedMonth);
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        DateTime? selectedYear = DateTimeHelper.AddYears(SelectedYear, 10);
                        OnSelectedYearChanged(selectedYear);
                        break;
                    }
            }
        }
        internal void ProcessPageUpKey(bool shift)
        {
            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        DateTime? selectedDate = DateTimeHelper.AddMonths(LastSelectedDate.GetValueOrDefault(DateTime.Today), -1);
                        ProcessSelection(shift, selectedDate, null);
                        break;
                    }
                case CalendarMode.Year:
                    {
                        DateTime? selectedMonth = DateTimeHelper.AddYears(_selectedMonth, -1);
                        OnSelectedMonthChanged(selectedMonth);
                        break;
                    }
                case CalendarMode.Decade:
                    {
                        DateTime? selectedYear = DateTimeHelper.AddYears(SelectedYear, -10);
                        OnSelectedYearChanged(selectedYear);
                        break;
                    }
            }
        }
        private void Calendar_KeyUp(KeyEventArgs e)
        {
            if (!e.Handled && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                ProcessShiftKeyUp();
            }
        }
        internal void ProcessShiftKeyUp()
        {
            if (_isShiftPressed && (SelectionMode == CalendarSelectionMode.SingleRange || SelectionMode == CalendarSelectionMode.MultipleRange))
            {
                AddSelection();
                _isShiftPressed = false;
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            HasFocusInternal = true;

            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        DateTime focusDate;
                        if (LastSelectedDate.HasValue && DateTimeHelper.CompareYearMonth(DisplayDateInternal, LastSelectedDate.Value) == 0)
                        {
                            focusDate = LastSelectedDate.Value;
                        }
                        else
                        {
                            focusDate = DisplayDate;
                            LastSelectedDate = DisplayDate;
                        }
                        Debug.Assert(focusDate != null, "focusDate should not be null!");
                        FocusButton = FindDayButtonFromDay(focusDate);

                        if (FocusButton != null)
                        {
                            FocusButton.IsCurrent = true;
                        }
                        break;
                    }
                case CalendarMode.Year:
                case CalendarMode.Decade:
                    {
                        if (this.FocusCalendarButton != null)
                        {
                            FocusCalendarButton.IsCalendarButtonFocused = true;
                        }
                        break;
                    }
            }
        }
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            HasFocusInternal = false;

            switch (DisplayMode)
            {
                case CalendarMode.Month:
                    {
                        if (FocusButton != null)
                        {
                            FocusButton.IsCurrent = false;
                        }
                        break;
                    }
                case CalendarMode.Year:
                case CalendarMode.Decade:
                    {
                        if (FocusCalendarButton != null)
                        {
                            FocusCalendarButton.IsCalendarButtonFocused = false;
                        }
                        break;
                    }
            }
        }
        /// <summary>
        ///  Called when the IsEnabled property changes.
        /// </summary>
        /// <param name="e">Property changed args.</param>
        private void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
        {
            Debug.Assert(e.NewValue is bool, "NewValue should be a boolean!");
            bool isEnabled = (bool)e.NewValue;

            if (MonthControl != null)
            {
                MonthControl.UpdateDisabled(isEnabled);
            }
        }

        static Calendar()
        {
            IsEnabledProperty.Changed.AddClassHandler<Calendar>(x => x.OnIsEnabledChanged);
            FirstDayOfWeekProperty.Changed.AddClassHandler<Calendar>(x => x.OnFirstDayOfWeekChanged);
            IsTodayHighlightedProperty.Changed.AddClassHandler<Calendar>(x => x.OnIsTodayHighlightedChanged);
            DisplayModeProperty.Changed.AddClassHandler<Calendar>(x => x.OnDisplayModePropertyChanged);
            SelectionModeProperty.Changed.AddClassHandler<Calendar>(x => x.OnSelectionModeChanged);
            SelectedDateProperty.Changed.AddClassHandler<Calendar>(x => x.OnSelectedDateChanged);
            DisplayDateProperty.Changed.AddClassHandler<Calendar>(x => x.OnDisplayDateChanged);
            DisplayDateStartProperty.Changed.AddClassHandler<Calendar>(x => x.OnDisplayDateStartChanged);
            DisplayDateEndProperty.Changed.AddClassHandler<Calendar>(x => x.OnDisplayDateEndChanged);
            KeyDownEvent.AddClassHandler<Calendar>(x => x.Calendar_KeyDown);
            KeyUpEvent.AddClassHandler<Calendar>(x => x.Calendar_KeyUp);
            
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:System.Windows.Controls.Calendar" /> class.
        /// </summary>
        public Calendar()
        {
            UpdateDisplayDate(this, this.DisplayDate, DateTime.MinValue);
            BlackoutDates = new CalendarBlackoutDatesCollection(this);
            SelectedDates = new SelectedDatesCollection(this);
            RemovedItems = new Collection<DateTime>();
        }

        private const string PART_ElementRoot = "Root";
        private const string PART_ElementMonth = "CalendarItem";
        /// <summary>
        /// Builds the visual tree for the
        /// <see cref="T:System.Windows.Controls.Calendar" /> when a new
        /// template is applied.
        /// </summary>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            Root = e.NameScope.Find<Panel>(PART_ElementRoot);

            SelectedMonth = DisplayDate;
            SelectedYear = DisplayDate;

            if (Root != null)
            {
                CalendarItem month = e.NameScope.Find<CalendarItem>(PART_ElementMonth);

                if (month != null)
                {
                    month.Owner = this;
                }
            }
        }

    }
}
