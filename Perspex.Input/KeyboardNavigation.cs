// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigation.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System.Linq;
    using Perspex.VisualTree;
    using Splat;

    public class KeyboardNavigation : IKeyboardNavigation
    {
        public static IKeyboardNavigation Instance
        {
            get { return Locator.Current.GetService<IKeyboardNavigation>(); }
        }

        public bool MoveNext(IInputElement element)
        {
            var parent = element.GetVisualParent();
            var descendent = element.GetVisualDescendents()
                .OfType<IInputElement>()
                .Where(x => x.Focusable && x.IsEnabledCore)
                .FirstOrDefault();

            if (descendent != null)
            {
                FocusManager.Instance.Focus(descendent, true);
                return true;
            }
            else if (parent != null)
            {
                var sibling = parent.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(x => x.Focusable && x.IsEnabledCore)
                    .SkipWhile(x => x != element)
                    .Skip(1)
                    .FirstOrDefault();

                if (sibling != null)
                {
                    FocusManager.Instance.Focus(sibling, true);
                    return true;
                }
            }

            return false;
        }

        public bool MovePrevious(IInputElement element)
        {
            var parent = element.GetVisualParent();
            var descendent = element.GetVisualDescendents()
                .OfType<IInputElement>()
                .Where(x => x.Focusable && x.IsEnabledCore)
                .Reverse()
                .FirstOrDefault();

            if (descendent != null)
            {
                FocusManager.Instance.Focus(descendent, true);
                return true;
            }
            else if (parent != null)
            {
                var previous = parent.GetVisualChildren()
                    .OfType<IInputElement>()
                    .Where(x => x.Focusable)
                    .Reverse()
                    .SkipWhile(x => x != element)
                    .Skip(1)
                    .FirstOrDefault();

                if (previous != null)
                {
                    FocusManager.Instance.Focus(previous, true);
                    return true;
                }
            }

            return false;
        }
    }
}
