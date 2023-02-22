using System;

#nullable enable

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Describes the action that caused a <see cref="IChildIndexProvider.ChildIndexChanged"/> event.
    /// </summary>
    public enum ChildIndexChangedAction
    {
        /// <summary>
        /// The index of a single child changed.
        /// </summary>
        ChildIndexChanged,

        /// <summary>
        /// The index of multiple children changed and all children should be re-evaluated.
        /// </summary>
        ChildIndexesReset,

        /// <summary>
        /// The total number of children changed.
        /// </summary>
        TotalCountChanged,
    }

    /// <summary>
    /// Event args for <see cref="IChildIndexProvider.ChildIndexChanged"/> event.
    /// </summary>
    public class ChildIndexChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildIndexChangedEventArgs"/> class with
        /// an action of <see cref="ChildIndexChangedAction.ChildIndexChanged"/>.
        /// </summary>
        /// <param name="child">The child whose index was changed.</param>
        /// <param name="index">The new index of the child.</param>
        public ChildIndexChangedEventArgs(ILogical child, int index)
        {
            Action = ChildIndexChangedAction.ChildIndexChanged;
            Child = child;
            Index = index;
        }

        private ChildIndexChangedEventArgs(ChildIndexChangedAction action)
        {
            Action = action;
            Index = -1;
        }

        /// <summary>
        /// Gets the type of change action that ocurred on the list control.
        /// </summary>
        public ChildIndexChangedAction Action { get; }

        /// <summary>
        /// Gets the logical child whose index was changed or null if all children should be re-evaluated.
        /// </summary>
        public ILogical? Child { get; }

        /// <summary>
        /// Gets the new index of <see cref="Child"/> or -1 if all children should be re-evaluated.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets an instance of the <see cref="ChildIndexChangedEventArgs"/> with an action of
        /// <see cref="ChildIndexChangedAction.ChildIndexesReset"/>.
        /// </summary>
        public static ChildIndexChangedEventArgs ChildIndexesReset { get; } = new(ChildIndexChangedAction.ChildIndexesReset);

        /// <summary>
        /// Gets an instance of the <see cref="ChildIndexChangedEventArgs"/> with an action of
        /// <see cref="ChildIndexChangedAction.TotalCountChanged"/>.
        /// </summary>
        public static ChildIndexChangedEventArgs TotalCountChanged { get; } = new(ChildIndexChangedAction.TotalCountChanged);
    }
}
