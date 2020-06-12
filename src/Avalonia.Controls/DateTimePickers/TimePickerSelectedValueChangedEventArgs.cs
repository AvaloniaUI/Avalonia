using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    public class TimePickerSelectedValueChangedEventArgs
    {
        public TimeSpan? OldTime { get; }
        public TimeSpan? NewTime { get; }
        public TimePickerSelectedValueChangedEventArgs(TimeSpan? old, TimeSpan? newT)
        {
            OldTime = old;
            NewTime = newT;
        }
    }
}
