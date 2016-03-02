// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Collections;

namespace Perspex.LogicalTree
{
    /// <summary>
    /// Represents a node in the logical tree.
    /// </summary>
    public interface ILogical
    {
        /// <summary>
        /// Raised when the control is attached to a rooted logical tree.
        /// </summary>
        event EventHandler<LogicalTreeAttachmentEventArgs> AttachedToLogicalTree;

        /// <summary>
        /// Raised when the control is detached from a rooted logical tree.
        /// </summary>
        event EventHandler<LogicalTreeAttachmentEventArgs> DetachedFromLogicalTree;

        /// <summary>
        /// Gets a value indicating whether the element is attached to a rooted logical tree.
        /// </summary>
        bool IsAttachedToLogicalTree { get; }

        /// <summary>
        /// Gets the logical parent.
        /// </summary>
        ILogical LogicalParent { get; }

        /// <summary>
        /// Gets the logical children.
        /// </summary>
        IPerspexReadOnlyList<ILogical> LogicalChildren { get; }
    }
}
