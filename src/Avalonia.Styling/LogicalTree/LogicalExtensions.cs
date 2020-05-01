using System;
using System.Collections.Generic;

namespace Avalonia.LogicalTree
{
    public static class LogicalExtensions
    {
        public static IEnumerable<ILogical> GetLogicalAncestors(this ILogical logical)
        {
            Contract.Requires<ArgumentNullException>(logical != null);

            logical = logical.LogicalParent;

            while (logical != null)
            {
                yield return logical;
                logical = logical.LogicalParent;
            }
        }

        public static IEnumerable<ILogical> GetSelfAndLogicalAncestors(this ILogical logical)
        {
            yield return logical;

            foreach (var ancestor in logical.GetLogicalAncestors())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        /// Finds first ancestor of given type.
        /// </summary>
        /// <typeparam name="T">Ancestor type.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <param name="includeSelf">If given logical should be included in search.</param>
        /// <returns>First ancestor of given type.</returns>
        public static T FindLogicalAncestorOfType<T>(this ILogical logical, bool includeSelf = false) where T : class
        {
            if (logical is null)
            {
                return null;
            }

            ILogical parent = includeSelf ? logical : logical.LogicalParent;

            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }

                parent = parent.LogicalParent;
            }

            return null;
        }

        public static IEnumerable<ILogical> GetLogicalChildren(this ILogical logical)
        {
            return logical.LogicalChildren;
        }

        public static IEnumerable<ILogical> GetLogicalDescendants(this ILogical logical)
        {
            foreach (ILogical child in logical.LogicalChildren)
            {
                yield return child;

                foreach (ILogical descendant in child.GetLogicalDescendants())
                {
                    yield return descendant;
                }
            }
        }

        public static IEnumerable<ILogical> GetSelfAndLogicalDescendants(this ILogical logical)
        {
            yield return logical;

            foreach (var descendent in logical.GetLogicalDescendants())
            {
                yield return descendent;
            }
        }

        /// <summary>
        /// Finds first descendant of given type.
        /// </summary>
        /// <typeparam name="T">Descendant type.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <param name="includeSelf">If given logical should be included in search.</param>
        /// <returns>First descendant of given type.</returns>
        public static T FindLogicalDescendantOfType<T>(this ILogical logical, bool includeSelf = false) where T : class
        {
            if (logical is null)
            {
                return null;
            }

            if (includeSelf && logical is T result)
            {
                return result;
            }

            return FindDescendantOfTypeCore<T>(logical);
        }

        public static ILogical GetLogicalParent(this ILogical logical)
        {
            return logical.LogicalParent;
        }

        public static T GetLogicalParent<T>(this ILogical logical) where T : class
        {
            return logical.LogicalParent as T;
        }

        public static IEnumerable<ILogical> GetLogicalSiblings(this ILogical logical)
        {
            ILogical parent = logical.LogicalParent;

            if (parent != null)
            {
                foreach (ILogical sibling in parent.LogicalChildren)
                {
                    yield return sibling;
                }
            }
        }

        public static bool IsLogicalAncestorOf(this ILogical logical, ILogical target)
        {
            ILogical current = target?.LogicalParent;

            while (current != null)
            {
                if (current == logical)
                {
                    return true;
                }

                current = current.LogicalParent;
            }

            return false;
        }

        private static T FindDescendantOfTypeCore<T>(ILogical logical) where T : class
        {
            var logicalChildren = logical.LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                ILogical child = logicalChildren[i];

                if (child is T result)
                {
                    return result;
                }

                var childResult = FindDescendantOfTypeCore<T>(child);

                if (!(childResult is null))
                {
                    return childResult;
                }
            }

            return null;
        }
    }
}
