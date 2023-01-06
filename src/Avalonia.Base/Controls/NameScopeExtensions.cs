using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.LogicalTree;

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
        public static T? Find<T>(this INameScope nameScope, string name)
            where T : class
        {
            _ = nameScope ?? throw new ArgumentNullException(nameof(nameScope));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var result = nameScope.Find(name);

            if (result == null)
            {
                return null;
            }

            if (result is T typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"Expected control '{name}' to be '{typeof(T)} but it was '{result.GetType()}'.");
        }

        /// <summary>
        /// Finds a named element in an <see cref="INameScope"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="anchor">The control to take the name scope from.</param>
        /// <param name="name">The name.</param>
        /// <returns>The named element or null if not found.</returns>
        public static T? Find<T>(this ILogical anchor, string name)
            where T : class
        {
            _ = anchor ?? throw new ArgumentNullException(nameof(anchor));
            _ = name ?? throw new ArgumentNullException(nameof(name));
            var styledAnchor = anchor as StyledElement;
            if (styledAnchor == null)
                return null;
            var nameScope = (anchor as INameScope) ?? NameScope.GetNameScope(styledAnchor);
            return nameScope?.Find<T>(name);
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
            _ = nameScope ?? throw new ArgumentNullException(nameof(nameScope));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var result = nameScope.Find(name);

            if (result == null)
            {
                throw new KeyNotFoundException($"Could not find control '{name}'.");
            }

            if (result is T typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"Expected control '{name}' to be '{typeof(T)} but it was '{result.GetType()}'.");
        }

        /// <summary>
        /// Gets a named element from an <see cref="INameScope"/> or throws if no element of the
        /// requested name was found.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="anchor">The control to take the name scope from.</param>
        /// <param name="name">The name.</param>
        /// <returns>The named element.</returns>
        public static T Get<T>(this ILogical anchor, string name)
            where T : class
        {
            _ = anchor ?? throw new ArgumentNullException(nameof(anchor));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var nameScope = (anchor as INameScope) ?? NameScope.GetNameScope((StyledElement)anchor);
            if (nameScope == null)
                throw new InvalidOperationException(
                    "The control doesn't have an associated name scope, probably no registrations has been done yet");
            
            return nameScope.Get<T>(name);
        }
        
        public static INameScope? FindNameScope(this ILogical control)
        {
            _ = control ?? throw new ArgumentNullException(nameof(control));

            var scope = control.GetSelfAndLogicalAncestors()
                .OfType<StyledElement>()
                .Select(x => (x as INameScope) ?? NameScope.GetNameScope(x))
                .FirstOrDefault(x => x != null);
            return scope;
        }
    }
}
