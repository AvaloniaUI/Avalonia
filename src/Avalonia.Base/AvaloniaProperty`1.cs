using System;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// A typed avalonia property.
    /// </summary>
    /// <typeparam name="TValue">The value type of the property.</typeparam>
    public abstract class AvaloniaProperty<TValue> : AvaloniaProperty
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        protected AvaloniaProperty(
            string name,
            Type ownerType,
            PropertyMetadata metadata,
            Action<IAvaloniaObject, bool> notifying = null)
            : base(name, typeof(TValue), ownerType, metadata, notifying)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        protected AvaloniaProperty(
            AvaloniaProperty source, 
            Type ownerType, 
            PropertyMetadata metadata)
            : base(source, ownerType, metadata)
        {
        }

        protected BindingValue<object> TryConvert(object value)
        {
            if (value == UnsetValue)
            {
                return BindingValue<object>.Unset;
            }
            else if (value == BindingOperations.DoNothing)
            {
                return BindingValue<object>.DoNothing;
            }

            if (!TypeUtilities.TryConvertImplicit(PropertyType, value, out var converted))
            {
                var error = new ArgumentException(string.Format(
                    "Invalid value for Property '{0}': '{1}' ({2})",
                    Name,
                    value,
                    value?.GetType().FullName ?? "(null)"));
                return BindingValue<object>.BindingError(error);
            }

            return converted;
        }
    }
}
