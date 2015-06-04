// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyChangedEventArgs.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    /// <summary>
    /// Provides information for a perspex property change.
    /// </summary>
    public class PerspexPropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="sender">The object that the property changed on.</param>
        /// <param name="property">The property that changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        public PerspexPropertyChangedEventArgs(
            PerspexObject sender,
            PerspexProperty property,
            object oldValue,
            object newValue,
            BindingPriority priority)
        {
            this.Sender = sender;
            this.Property = property;
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.Priority = priority;
        }

        /// <summary>
        /// Gets the <see cref="PerspexObject"/> that the property changed on.
        /// </summary>
        /// <value>The sender object.</value>
        public PerspexObject Sender { get; private set; }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public PerspexProperty Property { get; private set; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        /// <value>
        /// The old value of the property.
        /// </value>
        public object OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        /// <value>
        /// The new value of the property.
        /// </value>
        public object NewValue { get; private set; }

        /// <summary>
        /// Gets the priority of the binding that produced the value.
        /// </summary>
        /// <value>
        /// The priority of the binding that produced the value.
        /// </value>
        public BindingPriority Priority { get; private set; }
    }
}
