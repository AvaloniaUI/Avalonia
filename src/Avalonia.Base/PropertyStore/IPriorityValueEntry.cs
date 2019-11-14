using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal interface IPriorityValueEntry : IValue
    {
        BindingPriority Priority { get; }

        void Reparent(IValueSink sink);
    }

    internal interface IPriorityValueEntry<T> : IPriorityValueEntry, IValue<T>
    {
    }
}
