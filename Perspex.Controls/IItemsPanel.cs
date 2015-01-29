// -----------------------------------------------------------------------
// <copyright file="IItemsPanel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    /// <summary>
    /// Interface used by <see cref="ItemsControl"/> to set logical ownership of the panel's 
    /// children.
    /// </summary>
    /// <remarks>
    /// <see cref="ItemsControl"/> needs to set the logical parent of each of its items to itself.
    /// To do this, it uses this interface to instruct the panel that instead of setting the 
    /// logical parent for each child to the panel itself, it should set it to that of 
    /// <see cref="ChildLogicalParent"/>.
    /// </remarks>
    public interface IItemsPanel
    {
        /// <summary>
        /// Gets or sets the logical parent that should be set on children of the panel.
        /// </summary>
        ILogical ChildLogicalParent { get; set; }
    }
}
