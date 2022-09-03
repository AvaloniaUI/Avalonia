using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Provides information for an Avalonia property change.
    /// </summary>
    /// <seealso cref="AvaloniaPropertyChangedEventArgs{T}"/>
    public abstract class AvaloniaPropertyChangedEventArgs : EventArgs
    {
        public AvaloniaPropertyChangedEventArgs(
            AvaloniaObject sender,
            BindingPriority priority)
        {
            Sender = sender;
            Priority = priority;
            IsEffectiveValueChange = true;
        }

        internal AvaloniaPropertyChangedEventArgs(
            AvaloniaObject sender,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            Sender = sender;
            Priority = priority;
            IsEffectiveValueChange = isEffectiveValueChange;
        }

        /// <summary>
        /// Gets the <see cref="AvaloniaObject"/> that the property changed on.
        /// </summary>
        /// <value>The sender object.</value>
        public AvaloniaObject Sender { get; }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public AvaloniaProperty Property => GetProperty();

        /// <summary>
        /// Gets the old value of the property, or <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        public object? OldValue { get; protected set; }

        /// <summary>
        /// Gets the new value of the property, or <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        public object? NewValue { get; protected set; }

        /// <summary>
        /// Gets the priority of the binding that produced the value.
        /// </summary>
        /// <value>
        /// The priority of the new value.
        /// </value>
        public BindingPriority Priority { get; }

        internal bool IsEffectiveValueChange { get; private set; }

        protected abstract AvaloniaProperty GetProperty();
    }
}
