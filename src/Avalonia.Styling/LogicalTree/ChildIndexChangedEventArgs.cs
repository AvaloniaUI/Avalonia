#nullable enable
using System;

namespace Avalonia.LogicalTree
{
    public class ChildIndexChangedEventArgs : EventArgs
    {
        public ChildIndexChangedEventArgs()
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
