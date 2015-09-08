





namespace Perspex.Styling
{
    using System;

    /// <summary>
    /// A setter for a <see cref="Style"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Setter"/> is used to set a <see cref="PerspexProperty"/> value on a
    /// <see cref="PerspexObject"/> depending on a condition.
    /// </remarks>
    public class Setter : ISetter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        public Setter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The property value.</param>
        public Setter(PerspexProperty property, object value)
        {
            this.Property = property;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the property to set.
        /// </summary>
        public PerspexProperty Property
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public object Value
        {
            get;
            set;
        }

        /// <summary>
        /// Applies the setter to the control.
        /// </summary>
        /// <param name="style">The style that is being applied.</param>
        /// <param name="control">The control.</param>
        /// <param name="activator">An optional activator.</param>
        public void Apply(IStyle style, IStyleable control, IObservable<bool> activator)
        {
            if (activator == null)
            {
                control.SetValue(this.Property, this.Value, BindingPriority.Style);
            }
            else
            {
                var binding = new StyleBinding(activator, this.Value, style.ToString());
                control.Bind(this.Property, binding, BindingPriority.StyleTrigger);
            }
        }
    }
}
