using Avalonia;
using System;

namespace Avalonia.Controls
{
    public sealed class DatePickerPresenterItem : AvaloniaObject
    {
        internal DatePickerPresenterItem(DateTimeOffset date)
        {
            _date = date;
        }

        public static readonly StyledProperty<string> DisplayTextProperty =
            AvaloniaProperty.Register<DatePickerPresenterItem, string>("DisplayText");

        public string DisplayText
        {
            get => GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        internal DateTimeOffset GetStoredDate() => _date;
        internal void UpdateStoredDate(DateTimeOffset newDate, string text)
        {
            _date = newDate;
            DisplayText = text;
        }

        private DateTimeOffset _date;
    }
}
