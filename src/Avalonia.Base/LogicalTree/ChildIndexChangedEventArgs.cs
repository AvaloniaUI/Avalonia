#nullable enable
using System;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Event args for <see cref="IChildIndexProvider.ChildIndexChanged"/> event.
    /// </summary>
    public class ChildIndexChangedEventArgs : EventArgs
    {
        public static new ChildIndexChangedEventArgs Empty { get; } = new ChildIndexChangedEventArgs();

        private ChildIndexChangedEventArgs()
        {
            Index = -1;
        }

        public ChildIndexChangedEventArgs(ILogical child, int index)
        {
            Child = child;
            Index = index;
        }

        /// <summary>
        /// Gets the logical child whose index was changed or null if all children should be re-evaluated.
        /// </summary>
        public ILogical? Child { get; }

        /// <summary>
        /// Gets the new index of <see cref="Child"/> or -1 if all children should be re-evaluated.
        /// </summary>
        public int Index { get; }
    }
}
