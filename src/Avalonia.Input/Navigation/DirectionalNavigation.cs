// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input.Navigation
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
            NavigationDirection direction)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            Contract.Requires<ArgumentException>(
                direction != NavigationDirection.Next &&
                direction != NavigationDirection.Previous);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                var mode = KeyboardNavigation.GetDirectionalNavigation((InputElement)container);

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetNextInContainer(element, container, direction) ??
                               GetFirstInNextContainer(element, element, direction);
                    case KeyboardNavigationMode.Cycle:
                        return GetNextInContainer(element, container, direction) ??
                               GetFocusableDescendant(container, direction);
                    case KeyboardNavigationMode.Contained:
                        return GetNextInContainer(element, container, direction);
                    default:
                        return null;
                }
            }
            else
            {
                return GetFocusableDescendants(element).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns a value indicting whether the specified direction is forward.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>True if the direction is forward.</returns>
        private static bool IsForward(NavigationDirection direction)
        {
            return direction == NavigationDirection.Next ||
                   direction == NavigationDirection.Last ||
                   direction == NavigationDirection.Right ||
                   direction == NavigationDirection.Down;
        }

        /// <summary>
        /// Gets the first or last focusable descendant of the specified element.
        /// </summary>
        /// <param name="container">The element.</param>
        /// <param name="direction">The direction to search.</param>
        /// <returns>The element or null if not found.##</returns>
        private static IInputElement GetFocusableDescendant(IInputElement container, NavigationDirection direction)
        {
            return IsForward(direction) ?
                GetFocusableDescendants(container).FirstOrDefault() :
                GetFocusableDescendants(container).LastOrDefault();
        }

        /// <summary>
        /// Gets the focusable descendants of the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The element's focusable descendants.</returns>
        private static IEnumerable<IInputElement> GetFocusableDescendants(IInputElement element)
        {
            var children = element.GetVisualChildren().OfType<IInputElement>();

            foreach (var child in children)
            {
                if (child.CanFocus())
                {
                    yield return child;
                }

                if (child.CanFocusDescendants())
                {
                    foreach (var descendant in GetFocusableDescendants(child))
                    {
                        yield return descendant;
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
            NavigationDirection direction)
        {
            if (direction == NavigationDirection.Down)
            {
                var descendant = GetFocusableDescendants(element).FirstOrDefault();

                if (descendant != null)
                {
                    return descendant;
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

                if (element != null && direction == NavigationDirection.Up)
                {
                    var descendant = GetFocusableDescendants(element).LastOrDefault();

                    if (descendant != null)
                    {
                        return descendant;
                    }
                }

                return element;
            }

            return null;
        }

        /// <summary>
        /// Gets the first item that should be focused in the next container.
        /// </summary>
        /// <param name="element">The element being navigated away from.</param>
        /// <param name="container">The container.</param>
        /// <param name="direction">The direction of the search.</param>
        /// <returns>The first element, or null if there are no more elements.</returns>
        private static IInputElement GetFirstInNextContainer(
            IInputElement element,
            IInputElement container,
            NavigationDirection direction)
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
                    .Where(FocusExtensions.CanFocusDescendants);
                var sibling = isForward ? 
                    siblings.SkipWhile(x => x != container).Skip(1).FirstOrDefault() : 
                    siblings.TakeWhile(x => x != container).LastOrDefault();

                if (sibling != null)
                {
                    if (sibling is ICustomKeyboardNavigation custom)
                    {
                        var (handled, customNext) = custom.GetNext(element, direction);

                        if (handled)
                        {
                            return customNext;
                        }
                    }

                    if (sibling.CanFocus())
                    {
                        next = sibling;
                    }
                    else
                    {
                        next = isForward ?
                            GetFocusableDescendants(sibling).FirstOrDefault() :
                            GetFocusableDescendants(sibling).LastOrDefault();
                    }
                }

                if (next == null)
                {
                    next = GetFirstInNextContainer(element, parent, direction);
                }
            }
            else
            {
                next = isForward ?
                    GetFocusableDescendants(container).FirstOrDefault() :
                    GetFocusableDescendants(container).LastOrDefault();
            }

            return next;
        }
    }
}
