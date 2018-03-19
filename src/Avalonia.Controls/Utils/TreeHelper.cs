using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal static class TreeHelper
    {
        /// <summary>
        /// Walks the visual tree to determine if a particular child is contained within a parent DependencyObject.
        /// </summary>
        /// <param name="element">Parent DependencyObject</param>
        /// <param name="child">Child DependencyObject</param>
        /// <returns>True if the parent element contains the child</returns>
        internal static bool ContainsChild(this IVisual element, IVisual child)
        {
            if (element != null)
            {
                while (child != null)
                {
                    if (child == element)
                    {
                        return true;
                    }

                    // Walk up the visual tree.  If we hit the root, try using the framework element's
                    // parent.  We do this because Popups behave differently with respect to the visual tree,
                    // and it could have a parent even if the VisualTreeHelper doesn't find it.
                    IVisual parent = child.GetVisualParent();
                    if (parent == null)
                    {
                        if (child is IControl childElement)
                        {
                            parent = childElement.Parent;
                        }
                    }
                    child = parent;
                }
            }
            return false;
        }

        /// <summary>
        /// Walks the visual tree to determine if the currently focused element is contained within
        /// a parent DependencyObject.  The FocusManager's GetFocusedElement method is used to determine
        /// the currently focused element, which is updated synchronously.
        /// </summary>
        /// <param name="element">Parent DependencyObject</param>
        /// <returns>True if the currently focused element is within the visual tree of the parent</returns>
        internal static bool ContainsFocusedElement(this IVisual element)
        {
            return (element == null) ? false : element.ContainsChild(FocusManager.Instance.Current);
        }
    }
}
