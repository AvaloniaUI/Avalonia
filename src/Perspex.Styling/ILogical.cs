// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Collections;

namespace Perspex
{
    /// <summary>
    /// Represents a node in the logical tree.
    /// </summary>
    public interface ILogical
    {
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
