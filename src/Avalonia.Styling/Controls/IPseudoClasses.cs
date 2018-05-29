// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Exposes an interface for setting pseudoclasses on a <see cref="Classes"/> collection.
    /// </summary>
    public interface IPseudoClasses
    {
        /// <summary>
        /// Adds a pseudoclass to the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        void Add(string name);

        /// <summary>
        /// Removes a pseudoclass from the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        bool Remove(string name);
    }
}
