// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Collections;

namespace Perspex.Controls
{
    /// <summary>
    /// A control that can make its visual children the logical children of another control.
    /// </summary>
    public interface IReparentingControl : IControl
    {
        /// <summary>
        /// Requests that the visual children of the control use another control as their logical
        /// parent.
        /// </summary>
        /// <param name="logicalParent">
        /// The logical parent for the visual children of the control.
        /// </param>
        /// <param name="children">
        /// The <see cref="ILogical.LogicalChildren"/> collection to modify.
        /// </param>
        void ReparentLogicalChildren(ILogical logicalParent, IPerspexList<ILogical> children);
    }
}