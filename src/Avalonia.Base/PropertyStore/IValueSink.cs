using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal interface IValueSink
    {
        void ValueChanged<T>(
            StyledPropertyBase<T> property,
            BindingPriority priority,
            Optional<T> oldValue,
            BindingValue<T> newValue);

        void Completed(AvaloniaProperty property, IPriorityValueEntry entry);
    }
}
