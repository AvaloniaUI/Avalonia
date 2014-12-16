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
        /// <returns></returns>
        public PerspexObject Sender { get; private set; }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        public PerspexProperty Property { get; private set; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public object OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public object NewValue { get; private set; }

        /// <summary>
        /// Gets the priority of the binding that produced the value.
        /// </summary>
        public BindingPriority Priority { get; private set; }
    }
}
