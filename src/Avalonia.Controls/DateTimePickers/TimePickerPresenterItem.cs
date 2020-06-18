using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    public sealed class TimePickerPresenterItem : AvaloniaObject
    {
        internal TimePickerPresenterItem(TimeSpan date)
        {
            _date = date;
        }

        public static readonly StyledProperty<string> DisplayTextProperty =
            AvaloniaProperty.Register<TimePickerPresenterItem, string>("DisplayText");

        public string DisplayText
        {
            get => GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        internal TimeSpan GetStoredTime() => _date;

        private TimeSpan _date;
    }
}
