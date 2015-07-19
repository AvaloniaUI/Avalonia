// -----------------------------------------------------------------------
// <copyright file="IVisualTreeHost.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.VisualTree
{
    /// <summary>
    /// Interface for controls that host their own separate visual tree, such as popups.
    /// </summary>
    public interface IVisualTreeHost
    {
        /// <summary>
        /// Gets the root of the hosted visual tree.
        /// </summary>
        /// <value>
        /// The root of the hosted visual tree.
        /// </value>
        IVisual Root { get; }
    }
}
