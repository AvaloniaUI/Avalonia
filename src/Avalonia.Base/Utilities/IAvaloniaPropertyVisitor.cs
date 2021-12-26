namespace Avalonia.Utilities
{
    /// <summary>
    /// A visitor to resolve an untyped <see cref="AvaloniaProperty"/> to a typed property.
    /// </summary>
    /// <typeparam name="TData">The type of user data passed.</typeparam>
    /// <remarks>
    /// Pass an instance that implements this interface to
    /// <see cref="AvaloniaProperty.Accept{TData}(IAvaloniaPropertyVisitor{TData}, ref TData)"/>
    /// in order to resolve un untyped <see cref="AvaloniaProperty"/> to a typed
    /// <see cref="StyledPropertyBase{TValue}"/> or <see cref="DirectPropertyBase{TValue}"/>.
    /// </remarks>
    public interface IAvaloniaPropertyVisitor<TData>
        where TData : struct
    {
        /// <summary>
        /// Called when the property is a styled property.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="data">The user data.</param>
        void Visit<T>(StyledPropertyBase<T> property, ref TData data);

        /// <summary>
        /// Called when the property is a direct property.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="data">The user data.</param>
        void Visit<T>(DirectPropertyBase<T> property, ref TData data);
    }
}
