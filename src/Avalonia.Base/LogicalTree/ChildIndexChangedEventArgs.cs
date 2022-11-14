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
        }

        public ChildIndexChangedEventArgs(ILogical child)
        {
            Child = child;
        }

        /// <summary>
        /// Logical child which index was changed.
        /// If null, all children should be reset.
        /// </summary>
        public ILogical? Child { get; }
    }
}
