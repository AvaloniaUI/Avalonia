namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents an untyped interface to <see cref="IPriorityValueEntry{T}"/>.
    /// </summary>
    internal interface IPriorityValueEntry : IValue
    {
    }

    /// <summary>
    /// Represents an object that can act as an entry in a <see cref="PriorityValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    internal interface IPriorityValueEntry<T> : IPriorityValueEntry, IValue<T>
    {
        void Reparent(PriorityValue<T> parent);
    }
}
