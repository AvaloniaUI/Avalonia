using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public abstract class RangeBaseAutomationPeer : ControlAutomationPeer, IRangeValueProvider
    {
        public RangeBaseAutomationPeer(RangeBase owner)
            : base(owner) 
        {
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new RangeBase Owner => (RangeBase)base.Owner;
        public virtual bool IsReadOnly => false;
        public double Maximum => Owner.Maximum;
        public double Minimum => Owner.Minimum;
        public double Value => Owner.Value;
        public double SmallChange => Owner.SmallChange;
        public double LargeChange => Owner.LargeChange;
        
        public void SetValue(double value) => Owner.Value = value;

        protected virtual void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == RangeBase.MinimumProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty, e.OldValue, e.NewValue);
            else if (e.Property == RangeBase.MaximumProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty, e.OldValue, e.NewValue);
            else if (e.Property == RangeBase.ValueProperty)
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
        }
    }
}
