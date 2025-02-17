using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Provides information for a avalonia property change.
    /// </summary>
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
        public AvaloniaObject Sender { get; private set; }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public AvaloniaProperty Property => GetProperty();

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public object? OldValue => GetOldValue();

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public object? NewValue => GetNewValue();

        /// <summary>
        /// Gets the priority of the binding that produced the value.
        /// </summary>
        /// <value>
        /// The priority of the new value.
        /// </value>
        public BindingPriority Priority { get; private set; }

        internal bool IsEffectiveValueChange { get; private set; }
        
        /// <summary>
        /// Sets the Sender property.
        /// This is purely for reuse in some code paths where multiple allocations may occur.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        internal void SetSender(AvaloniaObject sender)
        {
            Sender = sender;
        }

        protected abstract AvaloniaProperty GetProperty();
        protected abstract object? GetOldValue();
        protected abstract object? GetNewValue();
    }
}
