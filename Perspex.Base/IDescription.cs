// -----------------------------------------------------------------------
// <copyright file="IDescription.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    /// <summary>
    /// Interface for objects with a <see cref="Description"/>.
    /// </summary>
    public interface IDescription
    {
        /// <summary>
        /// Gets the description of the object.
        /// </summary>
        /// <value>
        /// The description of the object.
        /// </value>
        string Description { get; }
    }
}
