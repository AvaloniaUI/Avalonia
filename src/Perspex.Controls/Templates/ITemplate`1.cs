// -----------------------------------------------------------------------
// <copyright file="ITemplate`1.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    /// <summary>
    /// Creates a control.
    /// </summary>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public interface ITemplate<TControl> where TControl : IControl
    {
        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <returns>
        /// The created control.
        /// </returns>
        TControl Build();
    }
}