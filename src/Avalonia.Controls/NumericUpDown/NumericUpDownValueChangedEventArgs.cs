using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    public class NumericUpDownValueChangedEventArgs : RoutedEventArgs
    {
        public NumericUpDownValueChangedEventArgs(RoutedEvent routedEvent, double oldValue,  double newValue) : base(routedEvent)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public double OldValue { get; }
        public double NewValue { get; }
    }
}
