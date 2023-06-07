using System;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <inheritdoc/>
    public partial class CalendarDatePicker
    {
        /// <summary>
        /// Defines the <see cref="DisplayDate"/> property.
        /// </summary>
        public static readonly StyledProperty<DateTime> DisplayDateProperty =
            AvaloniaProperty.Register<CalendarDatePicker, DateTime>(nameof(DisplayDate));

        /// <summary>
        /// Defines the <see cref="DisplayDateStart"/> property.
        /// </summary>
        public static readonly StyledProperty<DateTime?> DisplayDateStartProperty =
            AvaloniaProperty.Register<CalendarDatePicker, DateTime?>(
                nameof(DisplayDateStart));

        /// <summary>
        /// Defines the <see cref="DisplayDateEnd"/> property.
        /// </summary>
        public static readonly StyledProperty<DateTime?> DisplayDateEndProperty =
            AvaloniaProperty.Register<CalendarDatePicker, DateTime?>(
                nameof(DisplayDateEnd));

        /// <summary>
        /// Defines the <see cref="FirstDayOfWeek"/> property.
        /// </summary>
        public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
            AvaloniaProperty.Register<CalendarDatePicker, DayOfWeek>(nameof(FirstDayOfWeek));

        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDropDownOpenProperty =
            AvaloniaProperty.Register<CalendarDatePicker, bool>(
                nameof(IsDropDownOpen));

        /// <summary>
        /// Defines the <see cref="IsTodayHighlighted"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTodayHighlightedProperty =
            AvaloniaProperty.Register<CalendarDatePicker, bool>(nameof(IsTodayHighlighted));

        /// <summary>
        /// Defines the <see cref="SelectedDate"/> property.
        /// </summary>
        public static readonly StyledProperty<DateTime?> SelectedDateProperty =
            AvaloniaProperty.Register<CalendarDatePicker, DateTime?>(
                nameof(SelectedDate),
                enableDataValidation: true, 
                defaultBindingMode:BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedDateFormat"/> property.
        /// </summary>
        public static readonly StyledProperty<CalendarDatePickerFormat> SelectedDateFormatProperty =
            AvaloniaProperty.Register<CalendarDatePicker, CalendarDatePickerFormat>(
                nameof(SelectedDateFormat),
                defaultValue: CalendarDatePickerFormat.Short,
                validate: IsValidSelectedDateFormat);

        /// <summary>
        /// Defines the <see cref="CustomDateFormatString"/> property.
        /// </summary>
        public static readonly StyledProperty<string> CustomDateFormatStringProperty =
            AvaloniaProperty.Register<CalendarDatePicker, string>(
                nameof(CustomDateFormatString),
                defaultValue: "d",
                validate: IsValidDateFormatString);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<CalendarDatePicker, string?>(nameof(Text));

        /// <summary>
        /// Defines the <see cref="Watermark"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> WatermarkProperty =
            TextBox.WatermarkProperty.AddOwner<CalendarDatePicker>();

        /// <summary>
        /// Defines the <see cref="UseFloatingWatermark"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            TextBox.UseFloatingWatermarkProperty.AddOwner<CalendarDatePicker>();

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<CalendarDatePicker>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<CalendarDatePicker>();

        /// <summary>
        /// Gets a collection of dates that are marked as not selectable.
        /// </summary>
        /// <value>
        /// A collection of dates that cannot be selected. The default value is
        /// an empty collection.
        /// </value>
        public CalendarBlackoutDatesCollection? BlackoutDates { get; private set; }

        /// <summary>
        /// Gets or sets the date to display.
        /// </summary>
        /// <value>
        /// The date to display. The default is <see cref="DateTime.Today" />.
        /// </value>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The specified date is not in the range defined by
        /// <see cref="CalendarDatePicker.DisplayDateStart" />
        /// and
        /// <see cref="CalendarDatePicker.DisplayDateEnd" />.
        /// </exception>
        public DateTime DisplayDate
        {
            get => GetValue(DisplayDateProperty);
            set => SetValue(DisplayDateProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the first date to be displayed.
        /// </summary>
        /// <value>The first date to display.</value>
        public DateTime? DisplayDateStart
        {
            get => GetValue(DisplayDateStartProperty);
            set => SetValue(DisplayDateStartProperty, value);
        }

        /// <summary>
        /// Gets or sets the last date to be displayed.
        /// </summary>
        /// <value>The last date to display.</value>
        public DateTime? DisplayDateEnd
        {
            get => GetValue(DisplayDateEndProperty);
            set => SetValue(DisplayDateEndProperty, value);
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
            get => GetValue(FirstDayOfWeekProperty);
            set => SetValue(FirstDayOfWeekProperty, value);
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
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
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
            get => GetValue(IsTodayHighlightedProperty);
            set => SetValue(IsTodayHighlightedProperty, value);
        }

        /// <summary>
        /// Gets or sets the currently selected date.
        /// </summary>
        /// <value>
        /// The date currently selected. The default is null.
        /// </value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The specified date is not in the range defined by
        /// <see cref="CalendarDatePicker.DisplayDateStart" />
        /// and
        /// <see cref="CalendarDatePicker.DisplayDateEnd" />,
        /// or the specified date is in the
        /// <see cref="CalendarDatePicker.BlackoutDates" />
        /// collection.
        /// </exception>
        public DateTime? SelectedDate
        {
            get => GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Gets or sets the format that is used to display the selected date.
        /// </summary>
        /// <value>
        /// The format that is used to display the selected date. The default is
        /// <see cref="CalendarDatePickerFormat.Short" />.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// An specified format is not valid.
        /// </exception>
        public CalendarDatePickerFormat SelectedDateFormat
        {
            get => GetValue(SelectedDateFormatProperty);
            set => SetValue(SelectedDateFormatProperty, value);
        }

        public string CustomDateFormatString
        {
            get => GetValue(CustomDateFormatStringProperty);
            set => SetValue(CustomDateFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the text that is displayed by the <see cref="CalendarDatePicker" />.
        /// </summary>
        /// <value>
        /// The text displayed by the <see cref="CalendarDatePicker" />.
        /// </value>
        /// <exception cref="FormatException">
        /// The text entered cannot be parsed to a valid date, and the exception
        /// is not suppressed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The text entered parses to a date that is not selectable.
        /// </exception>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <inheritdoc cref="TextBox.Watermark"/>
        public string? Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <inheritdoc cref="TextBox.UseFloatingWatermark"/>
        public bool UseFloatingWatermark
        {
            get => GetValue(UseFloatingWatermarkProperty);
            set => SetValue(UseFloatingWatermarkProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }
    }
}
