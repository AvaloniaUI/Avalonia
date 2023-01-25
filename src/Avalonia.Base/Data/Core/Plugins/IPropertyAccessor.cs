using System;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Defines an accessor to a property on an object returned by a 
    /// <see cref="IPropertyAccessorPlugin"/>
    /// </summary>
    public interface IPropertyAccessor : IDisposable
    {
        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The accessor has not been subscribed to yet.
        /// </exception>
        Type? PropertyType { get; }

        /// <summary>
        /// Gets the current value of the property.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">
        /// The value to set. Guaranteed to be of a valid type for the property.
        /// </param>
        /// <param name="priority">
        /// The priority with which to set the value.
        /// </param>
        /// <returns>
        /// True if the property was set; false if the property could not be set.
        /// </returns>
        bool SetValue(object? value, BindingPriority priority);

        /// <summary>
        /// Subscribes to the value of the member.
        /// </summary>
        /// <param name="listener">A method that receives the values.</param>
        void Subscribe(Action<object?> listener);

        /// <summary>
        /// Unsubscribes to the value of the member.
        /// </summary>
        void Unsubscribe();
    }
}
