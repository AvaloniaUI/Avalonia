using System.Collections.Generic;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Represents a collection of property values in a <see cref="PropertyStore.ValueStore"/>.
    /// </summary>
    /// <remarks>
    /// A value frame is an abstraction over the following sources of values in an
    /// <see cref="AvaloniaObject"/>:
    /// 
    /// - A style
    /// - Local values
    /// - Animation values
    /// </remarks>
    internal interface IValueFrame
    {
        /// <summary>
        /// Gets a value indicating whether the frame is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the frame's priority.
        /// </summary>
        BindingPriority Priority { get; }

        /// <summary>
        /// Gets the frame's value entries.
        /// </summary>
        IList<IValueEntry> Values { get; }

        /// <summary>
        /// Sets the owner of the value frame.
        /// </summary>
        /// <param name="owner">The new owner.</param>
        void SetOwner(ValueStore? owner);
    }
}
