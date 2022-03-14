using System;

namespace Avalonia.Automation
{
    public class AutomationPropertyChangedEventArgs : EventArgs
    {
        public AutomationPropertyChangedEventArgs(
            AutomationProperty property,
            object? oldValue,
            object? newValue)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public AutomationProperty Property { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }
    }
}
