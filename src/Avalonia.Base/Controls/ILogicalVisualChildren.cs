using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Stores the logical and visual children for a control.
    /// </summary>
    public interface ILogicalVisualChildren
    {
        /// <summary>
        /// Gets the logical children of the control as a read-only list.
        /// </summary>
        IReadOnlyList<ILogical> Logical { get; }

        /// <summary>
        /// Gets the visual children of the control as a read-only list.
        /// </summary>
        IReadOnlyList<IVisual> Visual { get; }

        /// <summary>
        /// Gets the logical children of the control as a writable list.
        /// </summary>
        IList<ILogical> LogicalMutable { get; }

        /// <summary>
        /// Gets the visual children of the control as a writable list.
        /// </summary>
        IList<IVisual> VisualMutable { get; }

        /// <summary>
        /// Adds a handler to listen to changes in the logical children.
        /// </summary>
        /// <param name="handler">The handler.</param>
        void AddLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler handler);

        /// <summary>
        /// Removes a handler added by <see cref="AddLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler)"/>.
        /// </summary>
        /// <param name="handler">The handler.</param>
        void RemoveLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler handler);

        /// <summary>
        /// Adds a handler to listen to changes in the visual children.
        /// </summary>
        /// <param name="handler">The handler.</param>
        void AddVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler handler);

        /// <summary>
        /// Removes a handler added by <see cref="AddVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler)"/>.
        /// </summary>
        /// <param name="handler">The handler.</param>
        void RemoveVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler handler);
    }
}
