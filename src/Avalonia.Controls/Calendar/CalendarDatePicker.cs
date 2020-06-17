// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the
    /// <see cref="E:Avalonia.Controls.CalendarDatePicker.DateValidationError" />
    /// event.
    /// </summary>
    public class CalendarDatePickerDateValidationErrorEventArgs : EventArgs
    {
        private bool _throwException;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.CalendarDatePickerDateValidationErrorEventArgs" />
        /// class.
        /// </summary>
        /// <param name="exception">
        /// The initial exception from the
        /// <see cref="E:Avalonia.Controls.CalendarDatePicker.DateValidationError" />
        /// event.
        /// </param>
        /// <param name="text">
        /// The text that caused the
        /// <see cref="E:Avalonia.Controls.CalendarDatePicker.DateValidationError" />
        /// event.
        /// </param>
        public CalendarDatePickerDateValidationErrorEventArgs(Exception exception, string text)
        {
            this.Text = text;
            this.Exception = exception;
        }

        /// <summary>
        /// Gets the initial exception associated with the
        /// <see cref="E:Avalonia.Controls.CalendarDatePicker.DateValidationError" />
        /// event.
        /// </summary>
        /// <value>
        /// The exception associated with the validation failure.
        /// </value>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets the text that caused the
        /// <see cref="E:Avalonia.Controls.CalendarDatePicker.DateValidationError" />
        /// event.
        /// </summary>
        /// <value>
        /// The text that caused the validation failure.
        /// </value>
        public string Text { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// <see cref="P:Avalonia.Controls.CalendarDatePickerDateValidationErrorEventArgs.Exception" />
        /// should be thrown.
        /// </summary>
        /// <value>
        /// True if the exception should be thrown; otherwise, false.
        /// </value>
        /// <exception cref="T:System.ArgumentException">
        /// If set to true and
        /// <see cref="P:Avalonia.Controls.CalendarDatePickerDateValidationErrorEventArgs.Exception" />
        /// is null.
        /// </exception>
        public bool ThrowException
        {
            get { return this._throwException; }
            set
            {
                if (value && this.Exception == null)
                {
                    throw new ArgumentException("Cannot Throw Null Exception");
                }
                this._throwException = value;
            }
        }
    }

    /// <summary>
    /// Specifies date formats for a
    /// <see cref="T:Avalonia.Controls.CalendarDatePicker" />.
    /// </summary>
    public enum CalendarDatePickerFormat
    {
        /// <summary>
        /// Specifies that the date should be displayed using unabbreviated days
        /// of the week and month names.
        /// </summary>
        Long = 0,

        /// <summary>
        /// Specifies that the date should be displayed using abbreviated days
        /// of the week and month names.
        /// </summary>
        Short = 1,

        /// <summary>
        /// Specifies that the date should be displayed using a custom format string.
        /// </summary>
        Custom = 2
    }

    public class CalendarDatePicker : TemplatedControl
    {
        private const string ElementTextBox = "PART_TextBox";
        private const string ElementButton = "PART_Button";
        private const string ElementPopup = "PART_Popup";
        private const string ElementCalendar = "PART_Calendar";

        private Calendar _calendar;
        private string _defaultText;
        private Button _dropDownButton;
        //private Canvas _outsideCanvas;
        //private Canvas _outsidePopupCanvas;
        private Popup _popUp;
        private TextBox _textBox;
        private IDisposable _textBoxTextChangedSubscription;
        private IDisposable _buttonPointerPressedSubscription;

        private DateTime? _onOpenSelectedDate;
        private bool _settingSelectedDate;

        private DateTime _displayDate;
        private DateTime? _displayDateStart;
        private DateTime? _displayDateEnd;
        private bool _isDropDownOpen;
        private DateTime? _selectedDate;
        private string _text;
        private bool _suspendTextChangeHandler = false;
        private bool _isPopupClosing = false;
        private bool _ignoreButtonClick = false;

        /// <summary>
        /// Gets a collection of dates that are marked as not selectable.
        /// </summary>
        /// <value>
        /// A collection of dates that cannot be selected. The default value is
        /// an empty collection.
        /// </value>
        public CalendarBlackoutDatesCollection BlackoutDates { get; private set; }

        public static readonly DirectProperty<CalendarDatePicker, DateTime> DisplayDateProperty =
            AvaloniaProperty.RegisterDirect<CalendarDatePicker, DateTime>(
                nameof(DisplayDate),
                o => o.DisplayDate,
                (o, v) => o.DisplayDate = v);
        public static readonly DirectProperty<CalendarDatePicker, DateTime?> DisplayDateStartProperty =
            AvaloniaProperty.RegisterDirect<CalendarDatePicker, DateTime?>(
                nameof(DisplayDateStart),
                o => o.DisplayDateStart,
                (o, v) => o.DisplayDateStart = v);
        public static readonly DirectProperty<CalendarDatePicker, DateTime?> DisplayDateEndProperty =
            AvaloniaProperty.RegisterDirect<CalendarDatePicker, DateTime?>(
                nameof(DisplayDateEnd),
                o => o.DisplayDateEnd,
                (o, v) => o.DisplayDateEnd = v);
        public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
            AvaloniaProperty.Register<CalendarDatePicker, DayOfWeek>(nameof(FirstDayOfWeek));

        public static readonly DirectProperty<CalendarDatePicker, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<CalendarDatePicker, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        public static readonly StyledProperty<bool> IsTodayHighlightedProperty =
            AvaloniaProperty.Register<CalendarDatePicker, bool>(nameof(IsTodayHighlighted));
        public static readonly DirectProperty<CalendarDatePicker, DateTime?> SelectedDateProperty =
            AvaloniaProperty.RegisterDirect<CalendarDatePicker, DateTime?>(
                nameof(SelectedDate),
                o => o.SelectedDate,
                (o, v) => o.SelectedDate = v);

        public static readonly StyledProperty<CalendarDatePickerFormat> SelectedDateFormatProperty =
            AvaloniaProperty.Register<CalendarDatePicker, CalendarDatePickerFormat>(
                nameof(SelectedDateFormat),
                defaultValue: CalendarDatePickerFormat.Short,
                validate: IsValidSelectedDateFormat);

        public static readonly StyledProperty<string> CustomDateFormatStringProperty =
            AvaloniaProperty.Register<CalendarDatePicker, string>(
                nameof(CustomDateFormatString),
                defaultValue: "d",
                validate: IsValidDateFormatString);

        public static readonly DirectProperty<CalendarDatePicker, string> TextProperty =
            AvaloniaProperty.RegisterDirect<CalendarDatePicker, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);
        public static readonly StyledProperty<string> WatermarkProperty =
            TextBox.WatermarkProperty.AddOwner<CalendarDatePicker>();
        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            TextBox.UseFloatingWatermarkProperty.AddOwner<CalendarDatePicker>();


        /// <summary>
        /// Gets or sets the date to display.
        /// </summary>
        /// <value>
        /// The date to display. The default 
        /// <see cref="P:System.DateTime.Today" />.
        /// </value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The specified date is not in the range defined by
        /// <see cref="P:Avalonia.Controls.CalendarDatePicker.DisplayDateStart" />
        /// and
        /// <see cref="P:Avalonia.Controls.CalendarDatePicker.DisplayDateEnd" />.
        /// </exception>
        public DateTime DisplayDate
        {
            get { return _displayDate; }
            set { SetAndRaise(DisplayDateProperty, ref _displayDate, value); }
        }
        
        /// <summary>
        /// Gets or sets the first date to be displayed.
        /// </summary>
        /// <value>The first date to display.</value>
        public DateTime? DisplayDateStart
        {
            get { return _displayDateStart; }
            set { SetAndRaise(DisplayDateStartProperty, ref _displayDateStart, value); }
        }

        /// <summary>
        /// Gets or sets the last date to be displayed.
        /// </summary>
        /// <value>The last date to display.</value>
        public DateTime? DisplayDateEnd
        {
            get { return _displayDateEnd; }
            set { SetAndRaise(DisplayDateEndProperty, ref _displayDateEnd, value); }
        }

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
        /// Gets or sets a value indicating whether the drop-down
        /// <see cref="T:Avalonia.Controls.Calendar" /> is open or closed.
        /// </summary>
        /// <value>
        /// True if the <see cref="T:Avalonia.Controls.Calendar" /> is
        /// open; otherwise, false. The default is false.
        /// </value>
        public bool IsDropDownOpen
        {
            get { return _isDropDownOpen; }
            set { SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current date will be
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
        /// Gets or sets the currently selected date.
        /// </summary>
        /// <value>
        /// The date currently selected. The default is null.
        /// </value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The specified date is not in the range defined by
        /// <see cref="P:Avalonia.Controls.DatePicker.DisplayDateStart" />
        /// and
        /// <see cref="P:Avalonia.Controls.DatePicker.DisplayDateEnd" />,
        /// or the specified date is in the
        /// <see cref="P:Avalonia.Controls.DatePicker.BlackoutDates" />
        /// collection.
        /// </exception>
        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set { SetAndRaise(SelectedDateProperty, ref _selectedDate, value); }
        }

        /// <summary>
        /// Gets or sets the format that is used to display the selected date.
        /// </summary>
        /// <value>
        /// The format that is used to display the selected date. The default is
        /// <see cref="F:Avalonia.Controls.DatePickerFormat.Short" />.
        /// </value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// An specified format is not valid.
        /// </exception>
        public CalendarDatePickerFormat SelectedDateFormat
        {
            get { return GetValue(SelectedDateFormatProperty); }
            set { SetValue(SelectedDateFormatProperty, value); }
        }

        public string CustomDateFormatString
        {
            get { return GetValue(CustomDateFormatStringProperty); }
            set { SetValue(CustomDateFormatStringProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text that is displayed by the
        /// <see cref="T:Avalonia.Controls.DatePicker" />.
        /// </summary>
        /// <value>
        /// The text displayed by the
        /// <see cref="T:Avalonia.Controls.DatePicker" />.
        /// </value>
        /// <exception cref="T:System.FormatException">
        /// The text entered cannot be parsed to a valid date, and the exception
        /// is not suppressed.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The text entered parses to a date that is not selectable.
        /// </exception>
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }

        public string Watermark
        {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }
        public bool UseFloatingWatermark
        {
            get { return GetValue(UseFloatingWatermarkProperty); }
            set { SetValue(UseFloatingWatermarkProperty, value); }
        }

        /// <summary>
        /// Occurs when the drop-down
        /// <see cref="T:Avalonia.Controls.Calendar" /> is closed.
        /// </summary>
        public event EventHandler CalendarClosed;

        /// <summary>
        /// Occurs when the drop-down
        /// <see cref="T:Avalonia.Controls.Calendar" /> is opened.
        /// </summary>
        public event EventHandler CalendarOpened;

        /// <summary>
        /// Occurs when <see cref="P:Avalonia.Controls.DatePicker.Text" />
        /// is assigned a value that cannot be interpreted as a date.
        /// </summary>
        public event EventHandler<CalendarDatePickerDateValidationErrorEventArgs> DateValidationError;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Avalonia.Controls.CalendarDatePicker.SelectedDate" />
        /// property is changed.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectedDateChanged;

        static CalendarDatePicker()
        {
            FocusableProperty.OverrideDefaultValue<CalendarDatePicker>(true);

            DisplayDateProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnDisplayDateChanged(e));
            DisplayDateStartProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnDisplayDateStartChanged(e));
            DisplayDateEndProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnDisplayDateEndChanged(e));
            IsDropDownOpenProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnIsDropDownOpenChanged(e));
            SelectedDateProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnSelectedDateChanged(e));
            SelectedDateFormatProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnSelectedDateFormatChanged(e));
            CustomDateFormatStringProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnCustomDateFormatStringChanged(e));
            TextProperty.Changed.AddClassHandler<CalendarDatePicker>((x,e) => x.OnTextChanged(e));
        }
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.DatePicker" /> class.
        /// </summary>
        public CalendarDatePicker()
        {
            FirstDayOfWeek = DateTimeHelper.GetCurrentDateFormat().FirstDayOfWeek;
            _defaultText = string.Empty;
            DisplayDate = DateTime.Today;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_calendar != null)
            {
                _calendar.DayButtonMouseUp -= Calendar_DayButtonMouseUp;
                _calendar.DisplayDateChanged -= Calendar_DisplayDateChanged;
                _calendar.SelectedDatesChanged -= Calendar_SelectedDatesChanged;
                _calendar.PointerPressed -= Calendar_PointerPressed;
                _calendar.KeyDown -= Calendar_KeyDown;
            }
            _calendar = e.NameScope.Find<Calendar>(ElementCalendar);
            if (_calendar != null)
            {
                _calendar.SelectionMode = CalendarSelectionMode.SingleDate;
                _calendar.SelectedDate = SelectedDate;
                SetCalendarDisplayDate(DisplayDate);
                SetCalendarDisplayDateStart(DisplayDateStart);
                SetCalendarDisplayDateEnd(DisplayDateEnd);

                _calendar.DayButtonMouseUp += Calendar_DayButtonMouseUp;
                _calendar.DisplayDateChanged += Calendar_DisplayDateChanged;
                _calendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
                _calendar.PointerPressed += Calendar_PointerPressed;
                _calendar.KeyDown += Calendar_KeyDown;
                //_calendar.SizeChanged += new SizeChangedEventHandler(Calendar_SizeChanged);
                //_calendar.IsTabStop = true;

                var currentBlackoutDays = BlackoutDates;
                BlackoutDates = _calendar.BlackoutDates;
                if(currentBlackoutDays != null)
                {
                    foreach (var range in currentBlackoutDays)
                    {
                        BlackoutDates.Add(range);
                    }
                }
            }

            if (_popUp != null)
            {
                _popUp.Child = null;
                _popUp.Closed -= PopUp_Closed;
            }
            _popUp = e.NameScope.Find<Popup>(ElementPopup);
            if(_popUp != null)
            {
                _popUp.Closed += PopUp_Closed;
                if (IsDropDownOpen)
                {
                    OpenDropDown();
                }
            }

            if(_dropDownButton != null)
            {
                _dropDownButton.Click -= DropDownButton_Click;
                _buttonPointerPressedSubscription?.Dispose();
            }
            _dropDownButton = e.NameScope.Find<Button>(ElementButton);
            if(_dropDownButton != null)
            {
                _dropDownButton.Click += DropDownButton_Click;
                _buttonPointerPressedSubscription =
                    _dropDownButton.AddDisposableHandler(PointerPressedEvent, DropDownButton_PointerPressed, handledEventsToo: true);
            }

            if (_textBox != null)
            {
                _textBox.KeyDown -= TextBox_KeyDown;
                _textBox.GotFocus -= TextBox_GotFocus;
                _textBoxTextChangedSubscription?.Dispose();
            }
            _textBox = e.NameScope.Find<TextBox>(ElementTextBox);

            if(!SelectedDate.HasValue)
            {
                SetWaterMarkText();
            }

            if(_textBox != null)
            {
                _textBox.KeyDown += TextBox_KeyDown;
                _textBox.GotFocus += TextBox_GotFocus;
                _textBoxTextChangedSubscription = _textBox.GetObservable(TextBox.TextProperty).Subscribe(txt => TextBox_TextChanged());

                if(SelectedDate.HasValue)
                {
                    _textBox.Text = DateTimeToString(SelectedDate.Value);
                }
                else if(!String.IsNullOrEmpty(_defaultText))
                {
                    _textBox.Text = _defaultText;
                    SetSelectedDate();
                }
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedDateProperty)
            {
                DataValidationErrors.SetError(this, change.NewValue.Error);
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (!e.Handled && SelectedDate.HasValue && _calendar != null)
            {
                DateTime selectedDate = this.SelectedDate.Value;
                DateTime? newDate = DateTimeHelper.AddDays(selectedDate, e.Delta.Y > 0 ? -1 : 1);
                if (newDate.HasValue && Calendar.IsValidDateSelection(_calendar, newDate.Value))
                {
                    SelectedDate = newDate;
                    e.Handled = true;
                }
            }
        }
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            if(IsEnabled && _textBox != null && e.NavigationMethod == NavigationMethod.Tab)
            {
                _textBox.Focus();
                var text = _textBox.Text;
                if(!string.IsNullOrEmpty(text))
                {
                    _textBox.SelectionStart = 0;
                    _textBox.SelectionEnd = text.Length;
                }
            }
        }
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            SetSelectedDate();
        }
        
        private void SetCalendarDisplayDate(DateTime value)
        {
            if (DateTimeHelper.CompareYearMonth(_calendar.DisplayDate, value) != 0)
            {
                _calendar.DisplayDate = DisplayDate;
                if (DateTime.Compare(_calendar.DisplayDate, DisplayDate) != 0)
                {
                    DisplayDate = _calendar.DisplayDate;
                }
            }
        }
        private void OnDisplayDateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_calendar != null)
            {
                var value = (DateTime)e.NewValue;
                SetCalendarDisplayDate(value);
            }
        }
        private void SetCalendarDisplayDateStart(DateTime? value)
        {
            _calendar.DisplayDateStart = value;
            if (_calendar.DisplayDateStart.HasValue && DisplayDateStart.HasValue && DateTime.Compare(_calendar.DisplayDateStart.Value, DisplayDateStart.Value) != 0)
            {
                DisplayDateStart = _calendar.DisplayDateStart;
            }
        }
        private void OnDisplayDateStartChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_calendar != null)
            {
                var value = (DateTime?)e.NewValue;
                SetCalendarDisplayDateStart(value);
            }
        }
        private void SetCalendarDisplayDateEnd(DateTime? value)
        {
            _calendar.DisplayDateEnd = value;
            if (_calendar.DisplayDateEnd.HasValue && DisplayDateEnd.HasValue && DateTime.Compare(_calendar.DisplayDateEnd.Value, DisplayDateEnd.Value) != 0)
            {
                DisplayDateEnd = _calendar.DisplayDateEnd;
            }

        }
        private void OnDisplayDateEndChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_calendar != null)
            {
                var value = (DateTime?)e.NewValue;
                SetCalendarDisplayDateEnd(value);
            }
        }
        private void OnIsDropDownOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (bool)e.OldValue;
            var value = (bool)e.NewValue;

            if (_popUp != null && _popUp.Child != null)
            {
                if (value != oldValue)
                {
                    if (_calendar.DisplayMode != CalendarMode.Month)
                    {
                        _calendar.DisplayMode = CalendarMode.Month;
                    }

                    if (value)
                    {
                        OpenDropDown();
                    }
                    else
                    {
                        _popUp.IsOpen = false;
                        OnCalendarClosed(new RoutedEventArgs());
                    }
                }
            }
        }
        private void OnSelectedDateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var addedDate = (DateTime?)e.NewValue;
            var removedDate = (DateTime?)e.OldValue;

            if (_calendar != null && addedDate != _calendar.SelectedDate)
            {
                _calendar.SelectedDate = addedDate;
            }

            if (SelectedDate != null)
            {
                DateTime day = SelectedDate.Value;

                // When the SelectedDateProperty change is done from
                // OnTextPropertyChanged method, two-way binding breaks if
                // BeginInvoke is not used:
                Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _settingSelectedDate = true;
                    Text = DateTimeToString(day);
                    _settingSelectedDate = false;
                    OnDateSelected(addedDate, removedDate);
                });

                // When DatePickerDisplayDateFlag is TRUE, the SelectedDate
                // change is coming from the Calendar UI itself, so, we
                // shouldn't change the DisplayDate since it will automatically
                // be changed by the Calendar
                if ((day.Month != DisplayDate.Month || day.Year != DisplayDate.Year) && (_calendar == null || !_calendar.CalendarDatePickerDisplayDateFlag))
                {
                    DisplayDate = day;
                }
                if(_calendar != null)
                    _calendar.CalendarDatePickerDisplayDateFlag = false;
            }
            else
            {
                _settingSelectedDate = true;
                SetWaterMarkText();
                _settingSelectedDate = false;
                OnDateSelected(addedDate, removedDate);
            }
        }
        private void OnDateFormatChanged()
        {
            if (_textBox != null)
            {
                if (SelectedDate.HasValue)
                {
                    Text = DateTimeToString(SelectedDate.Value);
                }
                else if (string.IsNullOrEmpty(_textBox.Text))
                {
                    SetWaterMarkText();
                }
                else
                {
                    DateTime? date = ParseText(_textBox.Text);

                    if (date != null)
                    {
                        string s = DateTimeToString((DateTime)date);
                        Text = s;
                    }
                }
            }
        }
        private void OnSelectedDateFormatChanged(AvaloniaPropertyChangedEventArgs e)
        {
            OnDateFormatChanged();
        }
        private void OnCustomDateFormatStringChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if(SelectedDateFormat == CalendarDatePickerFormat.Custom)
            {
                OnDateFormatChanged();
            }
        }
        private void OnTextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (string)e.OldValue;
            var value = (string)e.NewValue;

            if (!_suspendTextChangeHandler)
            {
                if (value != null)
                {
                    if (_textBox != null)
                    {
                        _textBox.Text = value;
                    }
                    else
                    {
                        _defaultText = value;
                    }
                    if (!_settingSelectedDate)
                    {
                        SetSelectedDate();
                    }
                }
                else
                {
                    if (!_settingSelectedDate)
                    {
                        _settingSelectedDate = true;
                        SelectedDate = null;
                        _settingSelectedDate = false;
                    }
                }
            }
            else
            {
                SetWaterMarkText();
            }
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.CalendarDatePicker.DateValidationError" />
        /// event.
        /// </summary>
        /// <param name="e">
        /// A
        /// <see cref="T:Avalonia.Controls.CalendarDatePickerDateValidationErrorEventArgs" />
        /// that contains the event data.
        /// </param>
        protected virtual void OnDateValidationError(CalendarDatePickerDateValidationErrorEventArgs e)
        {
            DateValidationError?.Invoke(this, e);
        }
        private void OnDateSelected(DateTime? addedDate, DateTime? removedDate)
        {
            EventHandler<SelectionChangedEventArgs> handler = this.SelectedDateChanged;
            if (null != handler)
            {
                Collection<DateTime> addedItems = new Collection<DateTime>();
                Collection<DateTime> removedItems = new Collection<DateTime>();

                if (addedDate.HasValue)
                {
                    addedItems.Add(addedDate.Value);
                }

                if (removedDate.HasValue)
                {
                    removedItems.Add(removedDate.Value);
                }

                handler(this, new SelectionChangedEventArgs(SelectingItemsControl.SelectionChangedEvent, removedItems, addedItems));
            }
        }
        private void OnCalendarClosed(EventArgs e)
        {
            CalendarClosed?.Invoke(this, e);
        }
        private void OnCalendarOpened(EventArgs e)
        {
            CalendarOpened?.Invoke(this, e);
        }

        private void Calendar_DayButtonMouseUp(object sender, PointerReleasedEventArgs e)
        {
            Focus();
            IsDropDownOpen = false;
        }      
        private void Calendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            if (e.AddedDate != this.DisplayDate)
            {
                SetValue(DisplayDateProperty, (DateTime) e.AddedDate);
            }
        }
        private void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.Assert(e.AddedItems.Count < 2, "There should be less than 2 AddedItems!");

            if (e.AddedItems.Count > 0 && SelectedDate.HasValue && DateTime.Compare((DateTime)e.AddedItems[0], SelectedDate.Value) != 0)
            {
                SelectedDate = (DateTime?)e.AddedItems[0];
            }
            else
            {
                if (e.AddedItems.Count == 0)
                {
                    SelectedDate = null;
                    return;
                }

                if (!SelectedDate.HasValue)
                {
                    if (e.AddedItems.Count > 0)
                    {
                        SelectedDate = (DateTime?)e.AddedItems[0];
                    }
                }
            }
        }
        private void Calendar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                e.Handled = true;
            }
        }
        private void Calendar_KeyDown(object sender, KeyEventArgs e)
        {
            Calendar c = sender as Calendar;
            Contract.Requires<ArgumentNullException>(c != null);

            if (!e.Handled && (e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.Escape) && c.DisplayMode == CalendarMode.Month)
            {
                Focus();
                IsDropDownOpen = false;

                if (e.Key == Key.Escape)
                {
                    SelectedDate = _onOpenSelectedDate;
                }
            }
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            IsDropDownOpen = false;
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = ProcessDatePickerKey(e);
            }
        }
        private void TextBox_TextChanged()
        {
            if (_textBox != null)
            {
                _suspendTextChangeHandler = true;
                Text = _textBox.Text;
                _suspendTextChangeHandler = false;
            }
        }
        private void DropDownButton_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            _ignoreButtonClick = _isPopupClosing;
        }
        private void DropDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_ignoreButtonClick)
            {
                HandlePopUp();
            }
            else
            {
                _ignoreButtonClick = false;
            }
        }
        private void PopUp_Closed(object sender, PopupClosedEventArgs e)
        {
            IsDropDownOpen = false;

            if(!_isPopupClosing)
            {
                if (e.CloseEvent is PointerEventArgs pointerEvent)
                {
                    pointerEvent.Handled = true;
                }

                _isPopupClosing = true;
                Threading.Dispatcher.UIThread.InvokeAsync(() => _isPopupClosing = false);
            }
        }

        private void HandlePopUp()
        {
            if (IsDropDownOpen)
            {
                Focus();
                IsDropDownOpen = false;
            }
            else
            {
                ProcessTextBox();
            }
        }
        private void OpenDropDown()
        {
            if (_calendar != null)
            {
                _calendar.Focus();
                OpenPopUp();
                _calendar.ResetStates();
                OnCalendarOpened(new RoutedEventArgs());
            }
        }
        private void OpenPopUp()
        {
            _onOpenSelectedDate = SelectedDate;
            _popUp.IsOpen = true;
        }

        /// <summary>
        /// Input text is parsed in the correct format and changed into a
        /// DateTime object.  If the text can not be parsed TextParseError Event
        /// is thrown.
        /// </summary>
        /// <param name="text">Inherited code: Requires comment.</param>
        /// <returns>
        /// IT SHOULD RETURN NULL IF THE STRING IS NOT VALID, RETURN THE
        /// DATETIME VALUE IF IT IS VALID.
        /// </returns>
        private DateTime? ParseText(string text)
        {
            DateTime newSelectedDate;

            // TryParse is not used in order to be able to pass the exception to
            // the TextParseError event
            try
            {
                newSelectedDate = DateTime.Parse(text, DateTimeHelper.GetCurrentDateFormat());

                if (Calendar.IsValidDateSelection(this._calendar, newSelectedDate))
                {
                    return newSelectedDate;
                }
                else
                {
                    var dateValidationError = new CalendarDatePickerDateValidationErrorEventArgs(new ArgumentOutOfRangeException(nameof(text), "SelectedDate value is not valid."), text);
                    OnDateValidationError(dateValidationError);

                    if (dateValidationError.ThrowException)
                    {
                        throw dateValidationError.Exception;
                    }
                }
            }
            catch (FormatException ex)
            {
                CalendarDatePickerDateValidationErrorEventArgs textParseError = new CalendarDatePickerDateValidationErrorEventArgs(ex, text);
                OnDateValidationError(textParseError);

                if (textParseError.ThrowException)
                {
                    throw textParseError.Exception;
                }
            }
            return null;
        }
        private string DateTimeToString(DateTime d)
        {
            DateTimeFormatInfo dtfi = DateTimeHelper.GetCurrentDateFormat();

            switch (SelectedDateFormat)
            {
                case CalendarDatePickerFormat.Short:
                    return string.Format(CultureInfo.CurrentCulture, d.ToString(dtfi.ShortDatePattern, dtfi));
                case CalendarDatePickerFormat.Long:
                    return string.Format(CultureInfo.CurrentCulture, d.ToString(dtfi.LongDatePattern, dtfi));
                case CalendarDatePickerFormat.Custom:
                    return string.Format(CultureInfo.CurrentCulture, d.ToString(CustomDateFormatString, dtfi));
            }
            return null;
        }
        private bool ProcessDatePickerKey(KeyEventArgs e)
        {
            
            switch (e.Key)
            {
                case Key.Enter:
                    {
                        SetSelectedDate();
                        return true;
                    }
                case Key.Down:
                    { 
                        if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
                        {
                            HandlePopUp();
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }
        private void ProcessTextBox()
        {
            SetSelectedDate();
            IsDropDownOpen = true;
            _calendar.Focus();
        }
        private void SetSelectedDate()
        {
            if (_textBox != null)
            {
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    string s = _textBox.Text;

                    if (SelectedDate != null)
                    {
                        // If the string value of the SelectedDate and the
                        // TextBox string value are equal, we do not parse the
                        // string again if we do an extra parse, we lose data in
                        // M/d/yy format.
                        // ex: SelectedDate = DateTime(1008,12,19) but when
                        // "12/19/08" is parsed it is interpreted as
                        // DateTime(2008,12,19)
                        string selectedDate = DateTimeToString(SelectedDate.Value);
                        if (selectedDate == s)
                        {
                            return;
                        }
                    }
                    DateTime? d = SetTextBoxValue(s);
                    
                    if (SelectedDate != d)
                    {
                        SelectedDate = d;
                    }
                }
                else
                {
                    if (SelectedDate != null)
                    {
                        SelectedDate = null;
                    }
                }
            }
            else
            {
                DateTime? d = SetTextBoxValue(_defaultText);

                if (SelectedDate != d)
                {
                    SelectedDate = d;
                }
            }
        }
        private DateTime? SetTextBoxValue(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                SetValue(TextProperty, s);
                return SelectedDate;
            }
            else
            {
                DateTime? d = ParseText(s);
                if (d != null)
                {
                    SetValue(TextProperty, s);
                    return d;
                }
                else
                {
                    // If parse error: TextBox should have the latest valid
                    // SelectedDate value:
                    if (SelectedDate != null)
                    {
                        string newtext = this.DateTimeToString(SelectedDate.Value);
                        SetValue(TextProperty, newtext);
                        return SelectedDate;
                    }
                    else
                    {
                        SetWaterMarkText();
                        return null;
                    }
                }
            }
        }
        private void SetWaterMarkText()
        {
            if (_textBox != null)
            {
                if (string.IsNullOrEmpty(Watermark) && !UseFloatingWatermark)
                {
                    DateTimeFormatInfo dtfi = DateTimeHelper.GetCurrentDateFormat();
                    Text = string.Empty;
                    _defaultText = string.Empty;
                    var watermarkFormat = "<{0}>";
                    string watermarkText;

                    switch (SelectedDateFormat)
                    {
                        case CalendarDatePickerFormat.Long:
                            {
                                watermarkText = string.Format(CultureInfo.CurrentCulture, watermarkFormat, dtfi.LongDatePattern.ToString());
                                break;
                            }
                        case CalendarDatePickerFormat.Short:
                        default:
                            {
                                watermarkText = string.Format(CultureInfo.CurrentCulture, watermarkFormat, dtfi.ShortDatePattern.ToString());
                                break;
                            }
                    }
                    _textBox.Watermark = watermarkText;
                }
                else
                {
                    _textBox.ClearValue(TextBox.WatermarkProperty);
                }
            }
        }

        private static bool IsValidSelectedDateFormat(CalendarDatePickerFormat value)
        {
            return value == CalendarDatePickerFormat.Long
                || value == CalendarDatePickerFormat.Short
                || value == CalendarDatePickerFormat.Custom;
        }
        private static bool IsValidDateFormatString(string formatString)
        {
            return !string.IsNullOrWhiteSpace(formatString);
        }
        private static DateTime DiscardDayTime(DateTime d)
        {
            int year = d.Year;
            int month = d.Month;
            DateTime newD = new DateTime(year, month, 1, 0, 0, 0);
            return newD;
        }
        private static DateTime? DiscardTime(DateTime? d)
        {
            if (d == null)
            {
                return null;
            }
            else
            {
                DateTime discarded = (DateTime) d;
                int year = discarded.Year;
                int month = discarded.Month;
                int day = discarded.Day;
                DateTime newD = new DateTime(year, month, day, 0, 0, 0);
                return newD;
            }
        }
    }
}
