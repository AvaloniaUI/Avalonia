// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia
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
