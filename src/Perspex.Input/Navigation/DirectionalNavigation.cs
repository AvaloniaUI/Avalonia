// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.VisualTree;

namespace Perspex.Input.Navigation
{
    /// <summary>
    /// The implementation for default directional navigation.
    /// </summary>
    public static class DirectionalNavigation
    {
        /// <summary>
        /// Gets the next control in the specified navigation direction.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>
        /// The next element in the specified direction, or null if <paramref name="element"/>
        /// was the last in the requested direction.
        /// </returns>
        public static IInputElement GetNext(
            IInputElement element,
            FocusNavigationDirection direction)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            Contract.Requires<ArgumentException>(
                direction != FocusNavigationDirection.Next &&
                direction != FocusNavigationDirection.Previous);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                var isForward = IsForward(direction);
                var mode = KeyboardNavigation.GetDirectionalNavigation((InputElement)container);

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetNextInContainer(element, container, direction) ??
                               GetFirstInNextContainer(element, direction);
                    case KeyboardNavigationMode.Cycle:
                        return GetNextInContainer(element, container, direction) ??
                               GetFocusableDescendent(container, direction);
                    case KeyboardNavigationMode.Contained:
                        return GetNextInContainer(element, container, direction);
                    default:
                        return null;
                }
            }
            else
            {
                return GetFocusableDescendents(element).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns a value indicting whether the specified direction is forward.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>True if the direction is forward.</returns>
        private static bool IsForward(FocusNavigationDirection direction)
        {
            return direction == FocusNavigationDirection.Next ||
                   direction == FocusNavigationDirection.Last ||
                   direction == FocusNavigationDirection.Right ||
                   direction == FocusNavigationDirection.Down;
        }

        /// <summary>
        /// Gets the first or last focusable descendent of the specified element.
        /// </summary>
        /// <param name="container">The element.</param>
        /// <param name="direction">The direction to search.</param>
        /// <returns>The element or null if not found.##</returns>
        private static IInputElement GetFocusableDescendent(IInputElement container, FocusNavigationDirection direction)
        {
            return IsForward(direction) ?
                GetFocusableDescendents(container).FirstOrDefault() :
                GetFocusableDescendents(container).LastOrDefault();
        }

        /// <summary>
        /// Gets the focusable descendents of the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The element's focusable descendents.</returns>
        private static IEnumerable<IInputElement> GetFocusableDescendents(IInputElement element)
        {
            var mode = KeyboardNavigation.GetDirectionalNavigation((InputElement)element);
            var children = element.GetVisualChildren().OfType<IInputElement>();

            foreach (var child in children)
            {
                if (child.CanFocus())
                {
                    yield return child;
                }

                if (child.CanFocusDescendents())
                {
                    foreach (var descendent in GetFocusableDescendents(child))
                    {
                        yield return descendent;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next item that should be focused in the specified container.
        /// </summary>
        /// <param name="element">The starting element/</param>
        /// <param name="container">The container.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>The next element, or null if the element is the last.</returns>
        private static IInputElement GetNextInContainer(
            IInputElement element,
            IInputElement container,
            FocusNavigationDirection direction)
        {
            if (direction == FocusNavigationDirection.Down)
            {
                var descendent = GetFocusableDescendents(element).FirstOrDefault();

                if (descendent != null)
                {
                    return descendent;
                }
            }

            if (container != null)
            {
                var navigable = container as INavigableContainer;

                if (navigable != null)
                {
                    while (element != null)
                    {
                        element = navigable.GetControl(direction, element);

                        if (element != null && element.CanFocus())
                        {
                            break;
                        }
                    }
                }
                else
                {
                    // TODO: Do a spatial search here if the container doesn't implement
                    // INavigableContainer.
                    element = null;
                }

                if (element != null && direction == FocusNavigationDirection.Up)
                {
                    var descendent = GetFocusableDescendents(element).LastOrDefault();

                    if (descendent != null)
                    {
                        return descendent;
                    }
                }

                return element;
            }

            return null;
        }

        /// <summary>
        /// Gets the first item that should be focused in the next container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="direction">The direction of the search.</param>
        /// <returns>The first element, or null if there are no more elements.</returns>
        private static IInputElement GetFirstInNextContainer(
            IInputElement container,
            FocusNavigationDirection direction)
        {
            var parent = container.GetVisualParent<IInputElement>();
            var isForward = IsForward(direction);
            IInputElement next = null;

            if (parent != null)
            {
                if (!isForward && parent.CanFocus())
                {
                    return parent;
                }

                var siblings = parent.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(FocusExtensions.CanFocusDescendents);
                var sibling = isForward ? 
                    siblings.SkipWhile(x => x != container).Skip(1).FirstOrDefault() : 
                    siblings.TakeWhile(x => x != container).LastOrDefault();

                if (sibling != null)
                {
                    if (sibling.CanFocus())
                    {
                        next = sibling;
                    }
                    else
                    {
                        next = isForward ?
                            GetFocusableDescendents(sibling).FirstOrDefault() :
                            GetFocusableDescendents(sibling).LastOrDefault();
                    }
                }

                if (next == null)
                {
                    next = GetFirstInNextContainer(parent, direction);
                }
            }
            else
            {
                next = isForward ?
                    GetFocusableDescendents(container).FirstOrDefault() :
                    GetFocusableDescendents(container).LastOrDefault();
            }

            return next;
        }
    }
}
