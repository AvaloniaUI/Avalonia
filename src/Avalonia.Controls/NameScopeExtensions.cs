// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// Extension methods for <see cref="INameScope"/>.
    /// </summary>
    public static class NameScopeExtensions
    {
        /// <summary>
        /// Finds a named element in an <see cref="INameScope"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="nameScope">The name scope.</param>
        /// <param name="name">The name.</param>
        /// <returns>The named element or null if not found.</returns>
        public static T Find<T>(this INameScope nameScope, string name)
            where T : class
        {
            Contract.Requires<ArgumentNullException>(nameScope != null);
            Contract.Requires<ArgumentNullException>(name != null);

            var result = nameScope.Find(name);

            if (result != null && !(result is T))
            {
                throw new InvalidOperationException(
                    $"Expected control '{name}' to be '{typeof(T)} but it was '{result.GetType()}'.");
            }

            return (T)result;
        }

        /// <summary>
        /// Gets a named element from an <see cref="INameScope"/> or throws if no element of the
        /// requested name was found.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="nameScope">The name scope.</param>
        /// <param name="name">The name.</param>
        /// <returns>The named element.</returns>
        public static T Get<T>(this INameScope nameScope, string name)
            where T : class
        {
            Contract.Requires<ArgumentNullException>(nameScope != null);
            Contract.Requires<ArgumentNullException>(name != null);

            var result = nameScope.Find(name);

            if (result == null)
            {
                throw new KeyNotFoundException($"Could not find control '{name}'.");
            }

            if (!(result is T))
            {
                throw new InvalidOperationException(
                    $"Expected control '{name}' to be '{typeof(T)} but it was '{result.GetType()}'.");
            }

            return (T)result;
        }
    }
}
