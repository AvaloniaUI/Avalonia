using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the argument passed when the <see cref="DatePicker"/> SelectedDate changes
    /// </summary>
    public class DatePickerSelectedValueChangedEventArgs
    {
        public DateTimeOffset? NewDate { get; }
        public DateTimeOffset? OldDate { get; }

        public DatePickerSelectedValueChangedEventArgs(DateTimeOffset? oldDate, DateTimeOffset? newDate)
        {
            NewDate = newDate;
            OldDate = oldDate;
        }
    }
}
