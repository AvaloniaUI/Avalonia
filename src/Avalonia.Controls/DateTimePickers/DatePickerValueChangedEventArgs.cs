using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the argument passed when the Date changes on the <see cref="DatePickerPresenter"/>
    /// </summary>
    public class DatePickerValueChangedEventArgs
    {
        public DateTimeOffset NewDate { get; }
        public DateTimeOffset OldDate { get; }

        public DatePickerValueChangedEventArgs(DateTimeOffset oldDate, DateTimeOffset newDate)
        {
            NewDate = newDate;
            OldDate = OldDate;
        }
    }
}
