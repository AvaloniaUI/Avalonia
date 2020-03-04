using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped interface to <see cref="IValue{T}"/>.
    /// </summary>
    internal interface IValue
    {
        Optional<object> Value { get; }
        BindingPriority ValuePriority { get; }
    }

    /// <summary>
    /// Represents an object that can act as an entry in a <see cref="ValueStore"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    internal interface IValue<T> : IValue
    {
        new Optional<T> Value { get; }
    }
}
