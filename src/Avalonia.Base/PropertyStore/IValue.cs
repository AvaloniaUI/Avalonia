using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped interface to <see cref="IValue{T}"/>.
    /// </summary>
    internal interface IValue
    {
        Optional<object> GetValue();
        BindingPriority Priority { get; }
    }

    /// <summary>
    /// Represents an object that can act as an entry in a <see cref="ValueStore"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    internal interface IValue<T> : IValue
    {
        Optional<T> GetValue(BindingPriority maxPriority = BindingPriority.Animation);
    }
}
