using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Interface for getting/setting <see cref="AvaloniaProperty"/> values on an object.
    /// </summary>
    public interface IAvaloniaObject
    {
        /// <summary>
        /// Raised when a <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        event EventHandler<AvaloniaPropertyChangedEventArgs>? PropertyChanged;

        /// <summary>
        /// Clears an <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        void ClearValue(AvaloniaProperty property);

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        IObservable<object?> GetObservable(AvaloniaProperty property);

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        object? GetValue(AvaloniaProperty property);

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> value with the specified binding priority.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="minPriority">The minimum priority for the value.</param>
        /// <param name="maxPriority">The maximum priority for the value.</param>
        /// <remarks>
        /// Gets the value of the property, if set on this object with a priority between
        /// <paramref name="minPriority"/> and <paramref name="maxPriority"/> (inclusive),
        /// otherwise <see cref="AvaloniaProperty.UnsetValue"/>. Note that this method does not
        /// return property values that come from inherited or default values.
        /// </remarks>
        object? GetValueByPriority(
            AvaloniaProperty property,
            BindingPriority minPriority,
            BindingPriority maxPriority);

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is animating.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is animating, otherwise false.</returns>
        bool IsAnimating(AvaloniaProperty property);

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        bool IsSet(AvaloniaProperty property);

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(AvaloniaProperty property, object? value);

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        IDisposable Bind(
            AvaloniaProperty property,
            IObservable<object?> source,
            BindingPriority priority = BindingPriority.LocalValue);
    }
}
