using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    public class NumericUpDownValueChangedEventArgs : RoutedEventArgs
    {
        public NumericUpDownValueChangedEventArgs(RoutedEvent routedEvent, decimal? oldValue, decimal? newValue) : base(routedEvent)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public decimal? OldValue { get; }
        public decimal? NewValue { get; }
    }
}
