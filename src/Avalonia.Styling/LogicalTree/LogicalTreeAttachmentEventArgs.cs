// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Styling;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Holds the event arguments for the <see cref="ILogical.AttachedToLogicalTree"/> and 
    /// <see cref="ILogical.DetachedFromLogicalTree"/> events.
    /// </summary>
    public class LogicalTreeAttachmentEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicalTreeAttachmentEventArgs"/> class.
        /// </summary>
        /// <param name="root">The root of the logical tree.</param>
        public LogicalTreeAttachmentEventArgs(IStyleHost root)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            Root = root;
        }

        /// <summary>
        /// Gets the root of the logical tree that the control is being attached to or detached from.
        /// </summary>
        public IStyleHost Root { get; }
    }
}
