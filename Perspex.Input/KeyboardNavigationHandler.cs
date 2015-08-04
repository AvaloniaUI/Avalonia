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
        /// Gets the next element in tab order.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The next element in tab order.</returns>
        public static IInputElement GetNextInTabOrder(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                var mode = KeyboardNavigation.GetTabNavigation((InputElement)container);

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetNextInContainer(element, container) ??
                               GetFirstInNextContainer(container);
                    case KeyboardNavigationMode.Cycle:
                        return GetNextInContainer(element, container) ??
                               GetDescendents(container).FirstOrDefault();
                    case KeyboardNavigationMode.Contained:
                        return GetNextInContainer(element, container);
                    default:
                        return GetFirstInNextContainer(container);
                }
            }
            else
            {
                return GetDescendents(element).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the next element in tab order.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The next element in tab order.</returns>
        public static IInputElement GetPreviousInTabOrder(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                var mode = KeyboardNavigation.GetTabNavigation((InputElement)container);

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetPreviousInContainer(element, container) ??
                               GetLastInPreviousContainer(element);
                    case KeyboardNavigationMode.Cycle:
                        return GetPreviousInContainer(element, container) ??
                               GetDescendents(container).LastOrDefault();
                    case KeyboardNavigationMode.Contained:
                        return GetPreviousInContainer(element, container);
                    default:
                        return GetLastInPreviousContainer(container);
                }
            }
            else
            {
                return GetDescendents(element).LastOrDefault();
            }
        }

        /// <summary>
        /// Moves the focus to the next control in tab order.
        /// </summary>
        /// <param name="element">The current element.</param>
        public void TabNext(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var next = GetNextInTabOrder(element);

            if (next != null)
            {
                FocusManager.Instance.Focus(next, true);
            }
        }

        /// <summary>
        /// Moves the focus to the previous control in tab order.
        /// </summary>
        /// <param name="element">The current element.</param>
        public void TabPrevious(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var next = GetPreviousInTabOrder(element);

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
        /// Gets the focusable descendents of the specified element, depending on the element's
        /// <see cref="KeyboardNavigation.TabNavigationProperty"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The element's focusable descendents.</returns>
        private static IEnumerable<IInputElement> GetDescendents(IInputElement element)
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
                    foreach (var descendent in GetDescendents(child))
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
        /// <returns>The next element, or null if the element is the last.</returns>
        private static IInputElement GetNextInContainer(IInputElement element, IInputElement container)
        {
            var descendent = GetDescendents(element).FirstOrDefault();

            if (descendent != null)
            {
                return descendent;
            }
            else if (container != null)
            {
                var sibling = container.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(CanFocus)
                    .SkipWhile(x => x != element)
                    .Skip(1)
                    .FirstOrDefault();

                if (sibling != null)
                {
                    return sibling;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the previous item that should be focused in the specified container.
        /// </summary>
        /// <param name="element">The starting element/</param>
        /// <param name="container">The container.</param>
        /// <returns>The previous element, or null if the element is the first.</returns>
        private static IInputElement GetPreviousInContainer(IInputElement element, IInputElement container)
        {
            return container.GetVisualChildren()
                .OfType<IInputElement>()
                .Where(CanFocus)
                .TakeWhile(x => x != element)
                .LastOrDefault();
        }

        /// <summary>
        /// Gets the first item that should be focused in the next container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The first element, or null if there are no more elements.</returns>
        private static IInputElement GetFirstInNextContainer(IInputElement container)
        {
            var parent = container.GetVisualParent<IInputElement>();
            IInputElement next = null;

            if (parent != null)
            {
                var sibling = parent.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(CanFocusDescendent)
                    .SkipWhile(x => x != container)
                    .Skip(1)
                    .FirstOrDefault();

                if (sibling != null)
                {
                    if (CanFocus(sibling))
                    {
                        next = sibling;
                    }
                    else
                    {
                        next = GetDescendents(sibling).FirstOrDefault();
                    }
                }

                if (next == null)
                {
                    next = GetFirstInNextContainer(parent);
                }
            }
            else
            {
                next = GetDescendents(container).FirstOrDefault();
            }

            return next;
        }

        /// <summary>
        /// Gets the last item that should be focused in the previous container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The next element, or null if there are no more elements.</returns>
        private static IInputElement GetLastInPreviousContainer(IInputElement container)
        {
            var parent = container.GetVisualParent<IInputElement>();
            IInputElement next = null;

            if (parent != null)
            {
                var sibling = parent.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(CanFocusDescendent)
                    .TakeWhile(x => x != container)
                    .LastOrDefault();

                if (sibling != null)
                {
                    if (CanFocus(sibling))
                    {
                        next = sibling;
                    }
                    else
                    {
                        next = GetDescendents(sibling).LastOrDefault();
                    }
                }

                if (next == null)
                {
                    next = GetLastInPreviousContainer(parent);
                }
            }
            else
            {
                next = GetDescendents(container).LastOrDefault();
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

            if (e.Key == Key.Tab && current != null)
            {
                if ((KeyboardDevice.Instance.Modifiers & ModifierKeys.Shift) == 0)
                {
                    this.TabNext(current);
                }
                else
                {
                    this.TabPrevious(current);
                }
            }
        }
    }
}
