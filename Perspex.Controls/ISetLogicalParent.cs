// -----------------------------------------------------------------------
// <copyright file="ISetLogicalParent.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    /// <summary>
    /// Defines an interface through which a <see cref="Control"/>'s logical parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for advanced scenarios only.
    /// </remarks>
    public interface ISetLogicalParent
    {
        /// <summary>
        /// Sets the control's parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(IControl parent);
    }
}