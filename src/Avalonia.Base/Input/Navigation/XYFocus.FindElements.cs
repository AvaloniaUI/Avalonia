using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Input;

public partial class XYFocus
{
    private static void FindElements(
        PooledList<XYFocusParams> focusList,
        InputElement startRoot,
        InputElement? currentElement,
        InputElement? activeScroller,
        bool ignoreClipping,
        bool shouldConsiderXYFocusKeyboardNavigation)
    {
        var isScrolling = (activeScroller != null);
        var collection = startRoot.VisualChildren;

        var kidCount = collection.Count;

        for (var i = 0; i < kidCount; i++)
        {
            var child = collection[i] as InputElement;

            if (child == null)
                continue;

            var isEngagementEnabledButNotEngaged = GetIsFocusEngagementEnabled(child) && !GetIsFocusEngaged(child);

            if (child != currentElement && FocusManager.CanFocus(child)
                && GetBoundsForRanking(child, ignoreClipping) is {} bounds)
            {
                if (isScrolling)
                {
                    if (IsCandidateParticipatingInScroll(child, activeScroller) ||
                        !IsOccluded(child, bounds) ||
                        IsCandidateChildOfAncestorScroller(child, activeScroller))
                    {
                        focusList.Add(new XYFocusParams(child, bounds));
                    }
                }
                else
                {
                    focusList.Add(new XYFocusParams(child, bounds));
                }
            }

            if (IsValidFocusSubtree(child, shouldConsiderXYFocusKeyboardNavigation) && !isEngagementEnabledButNotEngaged)
            {
                FindElements(focusList, child, currentElement, activeScroller, ignoreClipping, shouldConsiderXYFocusKeyboardNavigation);
            }
        }
    }

    private static bool IsValidFocusSubtree(InputElement element, bool shouldConsiderXYFocusKeyboardNavigation)
    {
        var isDirectionalRegion =
            shouldConsiderXYFocusKeyboardNavigation &&
            IsDirectionalRegion(element);

        return element.IsEffectivelyVisible &&
               element.IsEffectivelyEnabled &&
               (!shouldConsiderXYFocusKeyboardNavigation || isDirectionalRegion);
    }

    /// Check if candidate's direct scroller is the same as active focused scroller.
    private static bool IsCandidateParticipatingInScroll(InputElement candidate, InputElement? activeScroller)
    {
        if (activeScroller == null)
        {
            return false;
        }

        var closestScroller = candidate.FindAncestorOfType<IInternalScroller>(true);
        return ReferenceEquals(closestScroller, activeScroller);
    }

    /// Check if there is a common parent scroller for both candidate and active scroller.
    private static bool IsCandidateChildOfAncestorScroller(InputElement candidate, InputElement? activeScroller)
    {
        if (activeScroller == null)
        {
            return false;
        }

        var parent = activeScroller.Parent;
        while (parent != null)
        {
            if (parent is IInternalScroller and Visual visual
                && visual.IsVisualAncestorOf(candidate))
            {
                return true;
            }
            parent = parent.Parent;
        }
        return false;
    }

    private static bool IsDirectionalRegion(InputElement? element)
    {
        if (element is null)
            return false;

        return GetKeyboardNavigationEnabled(element);
    }
    
    private static bool IsOccluded(InputElement element, Rect elementBounds)
    {
        // if (element is CHyperlink hyperlink)
        // {
        //     element = hyperlink.GetContainingFrameworkElement();
        // }

        var root = (InputElement)element.GetVisualRoot()!;
        
        // Check if the element is within the visible area of the window
        var visibleBounds = new Rect(0, 0, root.Bounds.Width, root.Bounds.Height);

        return !visibleBounds.Intersects(elementBounds);
    }

    private static Rect? GetBoundsForRanking(InputElement element, bool ignoreClipping)
    {
        if (element.GetTransformedBounds() is { } bounds)
        {
            return ignoreClipping
                ? bounds.Bounds.TransformToAABB(bounds.Transform)
                : bounds.Clip;
        }

        return null;
    }
}

