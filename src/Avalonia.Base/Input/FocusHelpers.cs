using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    internal static class FocusHelpers
    {
        public static IEnumerable<IInputElement> GetInputElementChildren(AvaloniaObject? parent)
        {
            // TODO: add control overrides to return custom focus list from control
            if (parent is Visual visual)
            {
                return visual.VisualChildren.OfType<IInputElement>();
            }

            return Array.Empty<IInputElement>();
        }

        public static bool CanHaveFocusableChildren(AvaloniaObject? parent)
        {
            if (parent == null)
                return false;

            var children = GetInputElementChildren(parent);

            bool hasFocusChildren = true;

            foreach (var child in children)
            {
                if (IsVisible(child))
                {
                    if (child.Focusable)
                    {
                        hasFocusChildren = true;
                    }
                    else if (CanHaveFocusableChildren(child as AvaloniaObject))
                    {
                        hasFocusChildren = true;
                    }
                }

                if (hasFocusChildren)
                    break;
            }

            return hasFocusChildren;
        }

        public static IInputElement? GetFocusParent(IInputElement? inputElement)
        {
            if (inputElement == null)
                return null;

            if (inputElement is Visual visual)
            {
                var rootVisual = visual.VisualRoot;
                if (inputElement != rootVisual)
                    return visual.Parent as IInputElement;
            }

            return null;
        }

        public static bool IsPotentialTabStop(IInputElement? element)
        {
            if (element is InputElement inputElement)
                return inputElement.IsTabStop;

            return false;
        }

        internal static bool IsVisible(IInputElement? element)
        {
            if(element is Visual visual)
                return visual.IsEffectivelyVisible;

            return false;
        }

        internal static bool IsFocusable(IInputElement? element)
        {
            return element?.Focusable ?? false;
        }

        internal static bool CanHaveChildren(IInputElement? element)
        {
            // We don't currently have a flag to indicate a visual can have children, so we just return whether the element is a visual
            return element is Visual;
        }
    }
}
