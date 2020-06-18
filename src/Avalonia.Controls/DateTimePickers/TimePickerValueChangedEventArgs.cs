using System;

namespace Avalonia.Controls
{
    public class TimePickerValueChangedEventArgs
    {
        public TimeSpan OldTime { get; }
        public TimeSpan NewTime { get; }
        public TimePickerValueChangedEventArgs(TimeSpan old, TimeSpan newT)
        {
            OldTime = old;
            NewTime = newT;
        }
    }
}
