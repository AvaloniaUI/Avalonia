using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal sealed class RemoveSentinelValue<T> : IValue
    {
        public static readonly RemoveSentinelValue<T> s_instance = new();
        
        public bool IsRemoveSentinel => true;
        public BindingPriority Priority => BindingPriority.Unset;
        public Optional<object?> GetValue()
        {
            return Optional<object?>.Empty;
        }

        public void Start()
        {
            throw new NotSupportedException();
        }

        public void RaiseValueChanged(AvaloniaObject owner, AvaloniaProperty property, Optional<object?> oldValue, Optional<object?> newValue)
        {
            owner.ValueChanged(new AvaloniaPropertyChangedEventArgs<T>(
                owner,
                (AvaloniaProperty<T>)property,
                oldValue.Cast<T>(),
                newValue.Cast<T>(),
                Priority));
        }

        public void BeginBatchUpdate()
        {
        }

        public void EndBatchUpdate()
        {
        }
    }
}
