// -----------------------------------------------------------------------
// <copyright file="IReparentingControl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Collections;

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