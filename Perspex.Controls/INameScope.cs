// -----------------------------------------------------------------------
// <copyright file="INameScope.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    /// <summary>
    /// Defines the interface for object which define a name scope.
    /// </summary>
    public interface INameScope
    {
        /// <summary>
        /// Returns an object tha thas the requested name.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The named object or null if the named object was not found.</returns>
        object FindName(string name);

        /// <summary>
        /// Registers an object with the specified name in the name scope.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <param name="o">The object.</param>
        /// <exception cref="ArgumentException">
        /// An object with the same name has already been registered.
        /// </exception>
        void RegisterName(string name, object o);

        /// <summary>
        /// Unregisters the specified name in the name scope.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <exception cref="ArgumentException">
        /// The name does not exist in the name scope.
        /// </exception>
        void UnregisterName(string name);
    }
}