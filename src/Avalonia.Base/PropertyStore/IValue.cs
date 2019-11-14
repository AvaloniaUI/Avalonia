using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal interface IValue
    {
        Optional<object> Value { get; }
        BindingPriority ValuePriority { get; }
    }

    internal interface IValue<T> : IValue
    {
        new Optional<T> Value { get; }
    }
}
