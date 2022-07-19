using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

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
    internal interface IValueFrame : IDisposable
    {
        /// <summary>
        /// Gets the number of value entries in the frame.
        /// </summary>
        int EntryCount { get; }

        /// <summary>
        /// Gets a value indicating whether the frame is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the value store that owns the frame.
        /// </summary>
        ValueStore? Owner { get; }

        /// <summary>
        /// Gets the frame's priority.
        /// </summary>
        BindingPriority Priority { get; }

        /// <summary>
        /// Retreives the frame's value entry with the specified index.
        /// </summary>
        IValueEntry GetEntry(int index);

        /// <summary>
        /// Sets the owner of the value frame.
        /// </summary>
        /// <param name="owner">The new owner.</param>
        void SetOwner(ValueStore? owner);

        /// <summary>
        /// Tries to retreive the frame's value entry for the specified property.
        /// </summary>
        bool TryGetEntry(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? entry);
    }
}
