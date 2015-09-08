
namespace Perspex.Diagnostics
{
    /// <summary>
    /// Holds diagnostic-related information about the value of a <see cref="PerspexProperty"/>
    /// on a <see cref="PerspexObject"/>.
    /// </summary>
    public class PerspexPropertyValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexPropertyValue"/> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The current property value.</param>
        /// <param name="priority">The priority of the current value.</param>
        /// <param name="diagnostic">A diagnostic string.</param>
        public PerspexPropertyValue(
            PerspexProperty property,
            object value,
            BindingPriority priority,
            string diagnostic)
        {
            this.Property = property;
            this.Value = value;
            this.Priority = priority;
            this.Diagnostic = diagnostic;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public PerspexProperty Property { get; }

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
