// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigation.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.VisualTree;
    using Splat;

    public class KeyboardNavigation : IKeyboardNavigation
    {
        public static readonly PerspexProperty<KeyboardNavigationMode> TabNavigationProperty =
            PerspexProperty.RegisterAttached<KeyboardNavigation, InputElement, KeyboardNavigationMode>("TabNavigation");

        public static readonly PerspexProperty<IInputElement> TabOnceActiveElementProperty =
            PerspexProperty.RegisterAttached<KeyboardNavigation, InputElement, IInputElement>("TabOnceActiveElement");

        public static IKeyboardNavigation Instance
        {
            get { return Locator.Current.GetService<IKeyboardNavigation>(); }
        }

        public static KeyboardNavigationMode GetTabNavigation(InputElement element)
        {
            return element.GetValue(TabNavigationProperty);
        }

        public static void SetTabNavigation(InputElement element, KeyboardNavigationMode value)
        {
            element.SetValue(TabNavigationProperty, value);
        }

        public static IInputElement GetTabOnceActiveElement(InputElement element)
        {
            return element.GetValue(TabOnceActiveElementProperty);
        }

        public static void SetTabOnceActiveElement(InputElement element, IInputElement value)
        {
            element.SetValue(TabOnceActiveElementProperty, value);
        }

        public IInputElement GetNextInTabOrder(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                var mode = GetTabNavigation((InputElement)container);

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetNextInContainer(element, container) ??
                               GetFirstInNextContainer(container);
                    case KeyboardNavigationMode.Cycle:
                        return GetNextInContainer(element, container) ??
                               GetDescendents(container).FirstOrDefault();
                    default:
                        return GetFirstInNextContainer(container);
                }
            }
            else
            {
                return GetDescendents(element).FirstOrDefault();
            }
        }

        public IInputElement GetPreviousInTabOrder(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var container = element.GetVisualParent<IInputElement>();

            if (container != null)
            {
                var mode = GetTabNavigation((InputElement)container);

                switch (mode)
                {
                    case KeyboardNavigationMode.Continue:
                        return GetPreviousInContainer(element, container) ??
                               GetLastInPreviousContainer(element);
                    case KeyboardNavigationMode.Cycle:
                        return GetPreviousInContainer(element, container) ??
                               GetDescendents(container).LastOrDefault();
                    default:
                        return GetLastInPreviousContainer(container);
                }
            }
            else
            {
                return GetDescendents(element).LastOrDefault();
            }
        }

        public void TabNext(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var next = GetNextInTabOrder(element);

            if (next != null)
            {
                TabTo(next);
            }
        }

        public void TabPrevious(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var next = GetPreviousInTabOrder(element);

            if (next != null)
            {
                TabTo(next);
            }
        }

        public void TabTo(IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            FocusManager.Instance.Focus(element, true);
        }

        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEnabledCore && e.IsVisible;

        private static bool CanFocusDescendent(IInputElement e) => e.IsEnabledCore && e.IsVisible;

        private static IEnumerable<IInputElement> GetDescendents(IInputElement element)
        {
            var mode = GetTabNavigation((InputElement)element);

            if (mode == KeyboardNavigationMode.Never)
            {
                yield break;
            }

            var children = element.GetVisualChildren().OfType<IInputElement>();

            if (mode == KeyboardNavigationMode.Once)
            {
                var active = GetTabOnceActiveElement((InputElement)element);

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

        private static IInputElement GetPreviousInContainer(IInputElement element, IInputElement container)
        {
            return container.GetVisualChildren()
                .OfType<IInputElement>()
                .Where(CanFocus)
                .TakeWhile(x => x != element)
                .LastOrDefault();
        }

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
    }
}
