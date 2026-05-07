using System;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class NumericUpDownAutomationPeer : ControlAutomationPeer, IRangeValueProvider
    {
        public NumericUpDownAutomationPeer(NumericUpDown owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new NumericUpDown Owner => (NumericUpDown)base.Owner;

        public bool IsReadOnly => Owner.IsReadOnly;

        public double Maximum => (double)Owner.Maximum;

        public double Minimum => (double)Owner.Minimum;

        public double Value => Owner.Value.HasValue
            ? (double)Owner.Value.Value
            : (double)Math.Clamp(0m, Owner.Minimum, Owner.Maximum);

        public double SmallChange => (double)Owner.Increment;

        public double LargeChange => (double)Owner.Increment;

        public void SetValue(double value)
        {
            Owner.Value = (decimal)value;
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

        protected override string GetClassNameCore() => "NumericUpDown";

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == NumericUpDown.MinimumProperty)
            {
                RaisePropertyChangedEvent(
                    RangeValuePatternIdentifiers.MinimumProperty,
                    e.OldValue,
                    e.NewValue);
            }
            else if (e.Property == NumericUpDown.MaximumProperty)
            {
                RaisePropertyChangedEvent(
                    RangeValuePatternIdentifiers.MaximumProperty,
                    e.OldValue,
                    e.NewValue);
            }
            else if (e.Property == NumericUpDown.ValueProperty)
            {
                RaisePropertyChangedEvent(
                    RangeValuePatternIdentifiers.ValueProperty,
                    e.OldValue,
                    e.NewValue);
            }
            else if (e.Property == NumericUpDown.IsReadOnlyProperty)
            {
                RaisePropertyChangedEvent(
                    RangeValuePatternIdentifiers.IsReadOnlyProperty,
                    e.OldValue,
                    e.NewValue);
            }
        }
    }
}
