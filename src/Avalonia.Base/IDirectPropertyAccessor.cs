using System;
using Avalonia.Metadata;

namespace Avalonia
{
    /// <summary>
    /// Provides a runtime interface for getting and setting 
    /// <see cref="DirectProperty{TOwner, TValue}"/> values.
    /// </summary>
    internal interface IDirectPropertyAccessor
    {
        /// <summary>
        /// Gets a value indicating whether the property is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the class that registered the property.
        /// </summary>
        Type Owner { get; }

        /// <summary>
        /// Gets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The property value.</returns>
        object? GetValue(AvaloniaObject instance);

        /// <summary>
        /// Sets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The value.</param>
        void SetValue(AvaloniaObject instance, object? value);

        /// <summary>
        /// Gets the unset value of the property for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        object? GetUnsetValue(Type type);
    }
}
