// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex
{
    /// <summary>
    /// Defines a name scope.
    /// </summary>
    public interface INameScope
    {
        /// <summary>
        /// Registers an element eith the name scope.
        /// </summary>
        /// <param name="name">The element name.</param>
        /// <param name="element">The element.</param>
        void Register(string name, object element);

        /// <summary>
        /// Finds a named element in the name scope.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The element, or null if the name was not found.</returns>
        object Find(string name);

        /// <summary>
        /// Unregisters an element with the name scope.
        /// </summary>
        /// <param name="name">The name.</param>
        void Unregister(string name);
    }
}
