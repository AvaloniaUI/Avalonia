// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigationHandler.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.VisualTree;

    /// <summary>
    /// Handles keyboard navigation for a window.
    /// </summary>
    public class KeyboardNavigationHandler : IKeyboardNavigationHandler
    {
        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot owner;

        /// <summary>
        /// Sets the owner of the keyboard navigation handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);

            if (this.owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            this.owner = owner;

            this.owner.AddHandler(InputElement.KeyDownEvent, this.OnKeyDown);
        }

        /// <summary>
        /// Gets the next control in the specified navigation direction.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>
        /// The next element in the specified direction, or null if <paramref name="element"/>
        /// was the last in therequested direction.
        /// </returns>
        public static IInputElement GetNext(
            IInputElement element,
            FocusNavigationDirection direction)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                KeyboardNavigationMode mode;

                if (direction == FocusNavigationDirection.Next || direction == FocusNavigationDirection.Previous)
                {
                    mode = KeyboardNavigation.GetTabNavigation((InputElement)container);
                }
                else
                {
                    mode = KeyboardNavigation.GetDirectionalNavigation((InputElement)container);
                }

                bool forward = direction == FocusNavigationDirection.Next ||
                               direction == FocusNavigationDirection.Last ||
                               direction == FocusNavigationDirection.Right ||
                               direction == FocusNavigationDirection.Down;

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetNextInContainer(element, container, direction) ??
                               GetFirstInNextContainer(element, forward);
                    case KeyboardNavigationMode.Cycle:
                        return GetNextInContainer(element, container, direction) ??
                               GetFocusableDescendent(container, forward);
                    case KeyboardNavigationMode.Contained:
                        return GetNextInContainer(element, container, direction);
                    default:
                        return GetFirstInNextContainer(container, forward);
                }
            }
            else
            {
                return GetFocusableDescendents(element).FirstOrDefault();
            }
        }

        /// <summary>
        /// Moves the focus in the specified direction.
        /// </summary>
        /// <param name="element">The current element.</param>
        /// <param name="direction">The direction to move.</param>
        public void Move(IInputElement element, FocusNavigationDirection direction)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var next = GetNext(element, direction);

            if (next != null)
            {
                FocusManager.Instance.Focus(next, true);
            }
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEnabledCore && e.IsVisible;

        /// <summary>
        /// Checks if a descendent of the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if a descendent of the element can be focused.</returns>
        private static bool CanFocusDescendent(IInputElement e) => e.IsEnabledCore && e.IsVisible;

        /// <summary>
        /// Gets the first or last focusable descendent of the specified element.
        /// </summary>
        /// <param name="container">The element.</param>
        /// <param name="forward">Whether to search forward or backwards.</param>
        /// <returns>The element or null if not found.##</returns>
        private static IInputElement GetFocusableDescendent(IInputElement container, bool forward)
        {
            return forward ?
                GetFocusableDescendents(container).FirstOrDefault() :
                GetFocusableDescendents(container).LastOrDefault();
        }

        /// <summary>
        /// Gets the focusable descendents of the specified element, depending on the element's
        /// <see cref="KeyboardNavigation.TabNavigationProperty"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The element's focusable descendents.</returns>
        private static IEnumerable<IInputElement> GetFocusableDescendents(IInputElement element)
        {
            var mode = KeyboardNavigation.GetTabNavigation((InputElement)element);

            if (mode == KeyboardNavigationMode.None)
            {
                yield break;
            }

            var children = element.GetVisualChildren().OfType<IInputElement>();

            if (mode == KeyboardNavigationMode.Once)
            {
                var active = KeyboardNavigation.GetTabOnceActiveElement((InputElement)element);

                if (active != null)
                {
                    yield return active;
                    yield break;
                }
                else
                {
                    children = children.Take(1);
                }
            }

            foreach (var child in children)
            {
                if (CanFocus(child))
                {
                    yield return child;
                }

                if (CanFocusDescendent(child))
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
            var descendent = GetFocusableDescendents(element).FirstOrDefault();

            if (descendent != null)
            {
                return descendent;
            }
            else if (container != null)
            {
                var navigable = container as INavigableContainer;

                // TODO: Do a spatial search here.
                if (navigable != null)
                {
                    while (element != null)
                    {
                        var sibling = navigable.GetControl(direction, element);

                        if (sibling != null && CanFocus(sibling))
                        {
                            return sibling;
                        }

                        element = sibling;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the first item that should be focused in the next container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="forward">Whether to search forward or backwards.</param>
        /// <returns>The first element, or null if there are no more elements.</returns>
        private static IInputElement GetFirstInNextContainer(IInputElement container, bool forward)
        {
            var parent = container.GetVisualParent<IInputElement>();
            IInputElement next = null;

            if (parent != null)
            {
                var siblings = parent.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(CanFocusDescendent);
                IInputElement sibling;

                if (forward)
                {
                    sibling = siblings.SkipWhile(x => x != container).Skip(1).FirstOrDefault();
                }
                else
                {
                    sibling = siblings.TakeWhile(x => x != container).LastOrDefault();
                }

                if (sibling != null)
                {
                    if (CanFocus(sibling))
                    {
                        next = sibling;
                    }
                    else
                    {
                        next = forward ?
                            GetFocusableDescendents(sibling).FirstOrDefault() :
                            GetFocusableDescendents(sibling).LastOrDefault();
                    }
                }

                if (next == null)
                {
                    next = GetFirstInNextContainer(parent, forward);
                }
            }
            else
            {
                next = forward ?
                    GetFocusableDescendents(container).FirstOrDefault() :
                    GetFocusableDescendents(container).LastOrDefault();
            }

            return next;
        }

        /// <summary>
        /// Handles the Tab key being pressed in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            var current = FocusManager.Instance.Current;

            if (current != null)
            {
                FocusNavigationDirection? direction = null;

                switch (e.Key)
                {
                    case Key.Tab:
                        direction = (KeyboardDevice.Instance.Modifiers & ModifierKeys.Shift) == 0 ?
                            FocusNavigationDirection.Next : FocusNavigationDirection.Previous;
                        break;
                    case Key.Up:
                        direction = FocusNavigationDirection.Up;
                        break;
                    case Key.Down:
                        direction = FocusNavigationDirection.Down;
                        break;
                    case Key.Left:
                        direction = FocusNavigationDirection.Left;
                        break;
                    case Key.Right:
                        direction = FocusNavigationDirection.Right;
                        break;
                }

                if (direction.HasValue)
                {
                    this.Move(current, direction.Value);
                    e.Handled = true;
                }
            }
        }
    }
}
