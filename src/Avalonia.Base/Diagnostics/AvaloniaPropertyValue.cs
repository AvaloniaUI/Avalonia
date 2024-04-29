using Avalonia.Data;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Holds diagnostic-related information about the value of an <see cref="AvaloniaProperty"/>
    /// on an <see cref="AvaloniaObject"/>.
    /// </summary>
    public sealed class AvaloniaPropertyValue
    {
        internal AvaloniaPropertyValue(
            AvaloniaProperty property,
            object? value,
            BindingPriority priority,
            string? diagnostic,
            bool isOverriddenCurrentValue)
        {
            Property = property;
            Value = value;
            Priority = priority;
            Diagnostic = diagnostic;
            IsOverriddenCurrentValue = isOverriddenCurrentValue;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public AvaloniaProperty Property { get; }

        /// <summary>
        /// Gets the current property value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the priority of the current value.
        /// </summary>
        public BindingPriority Priority { get; }

        /// <summary>
        /// Gets a diagnostic string.
        /// </summary>
        public string? Diagnostic { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> was overridden by a call to 
        /// <see cref="AvaloniaObject.SetCurrentValue{T}"/>.
        /// </summary>
        public bool IsOverriddenCurrentValue { get; }
    }
}
