// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Collections;

namespace Perspex.Controls
{
    /// <summary>
    /// A control that can use the visual children of another control as its logical children.
    /// </summary>
    public interface IReparentingHost : ILogical
    {
        /// <summary>
        /// Gets a writeable logical children collection from the host.
        /// </summary>
        new IPerspexList<ILogical> LogicalChildren { get; }

        /// <summary>
        /// Asks the control whether it wants to reparent the logical children of the specified
        /// control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>
        /// True if the control wants to reparent its logical children otherwise false.
        /// </returns>
        bool WillReparentChildrenOf(IControl control);
    }
}