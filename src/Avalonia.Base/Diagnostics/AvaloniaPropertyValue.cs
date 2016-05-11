// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Holds diagnostic-related information about the value of a <see cref="AvaloniaProperty"/>
    /// on a <see cref="AvaloniaObject"/>.
    /// </summary>
    public class AvaloniaPropertyValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyValue"/> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The current property value.</param>
        /// <param name="priority">The priority of the current value.</param>
        /// <param name="diagnostic">A diagnostic string.</param>
        public AvaloniaPropertyValue(
            AvaloniaProperty property,
            object value,
            BindingPriority priority,
            string diagnostic)
        {
            Property = property;
            Value = value;
            Priority = priority;
            Diagnostic = diagnostic;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public AvaloniaProperty Property { get; }

        /// <summary>
        /// Gets the current property value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the priority of the current value.
        /// </summary>
        public BindingPriority Priority { get; }

        /// <summary>
        /// Gets a diagnostic string.
        /// </summary>
        public string Diagnostic { get; }
    }
}
