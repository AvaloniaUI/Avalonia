using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.VisualTree;

namespace Avalonia.Input;

public partial class XYFocus
{
    internal static double CalculatePrimaryAxisDistance(
        NavigationDirection direction,
        Rect bounds,
        Rect candidateBounds)
    {
        double primaryAxisDistance = -1;
        var isOverlapping = bounds.Intersects(candidateBounds);

        if (bounds == candidateBounds) return -1; // We shouldn't be calculating the distance from ourselves

        if (direction == NavigationDirection.Left
            && (candidateBounds.Right <= bounds.Left || (isOverlapping && candidateBounds.Left <= bounds.Left)))
            primaryAxisDistance = Math.Abs(bounds.Left - candidateBounds.Right);
        else if (direction == NavigationDirection.Right
                 && (candidateBounds.Left >= bounds.Right || (isOverlapping && candidateBounds.Right >= bounds.Right)))
            primaryAxisDistance = Math.Abs(candidateBounds.Left - bounds.Right);
        else if (direction == NavigationDirection.Up
                 && (candidateBounds.Bottom <= bounds.Top || (isOverlapping && candidateBounds.Top <= bounds.Top)))
            primaryAxisDistance = Math.Abs(bounds.Top - candidateBounds.Bottom);
        else if (direction == NavigationDirection.Down
                 && (candidateBounds.Top >= bounds.Bottom || (isOverlapping && candidateBounds.Bottom >= bounds.Bottom)))
            primaryAxisDistance = Math.Abs(candidateBounds.Top - bounds.Bottom);

        return primaryAxisDistance;
    }

    internal static double CalculateSecondaryAxisDistance(
        NavigationDirection direction,
        Rect bounds,
        Rect candidateBounds)
    {
        double secondaryAxisDistance;

        if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
            // calculate secondary axis distance for the case where the element is not in the shadow
            secondaryAxisDistance = candidateBounds.Top < bounds.Top ?
                Math.Abs(bounds.Top - candidateBounds.Bottom) :
                Math.Abs(candidateBounds.Top - bounds.Bottom);
        else
            // calculate secondary axis distance for the case where the element is not in the shadow
            secondaryAxisDistance = candidateBounds.Left < bounds.Left ?
                Math.Abs(bounds.Left - candidateBounds.Right) :
                Math.Abs(candidateBounds.Left - bounds.Right);

        return secondaryAxisDistance;
    }

    /// Calculates the percentage of the potential element that is in the shadow of the reference element.
    internal static double CalculatePercentInShadow(
        (double, double) referenceManifold,
        (double, double) potentialManifold)
    {
        if (referenceManifold.Item1 > potentialManifold.Item2 || referenceManifold.Item2 <= potentialManifold.Item1)
            // Potential is not in the reference's shadow.
            return 0;

        var shadow = Math.Min(referenceManifold.Item2, potentialManifold.Item2) -
                     Math.Max(referenceManifold.Item1, potentialManifold.Item1);
        shadow = Math.Abs(shadow);

        var potentialEdgeLength = Math.Abs(potentialManifold.Item2 - potentialManifold.Item1);
        var referenceEdgeLength = Math.Abs(referenceManifold.Item2 - referenceManifold.Item1);

        var comparisonEdgeLength = referenceEdgeLength;

        if (comparisonEdgeLength >= potentialEdgeLength) comparisonEdgeLength = potentialEdgeLength;

        double percentInShadow = 1;

        if (comparisonEdgeLength != 0) percentInShadow = Math.Min(shadow / comparisonEdgeLength, 1.0);

        return percentInShadow;
    }

    internal static void FindElements(
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

    internal static bool IsValidFocusSubtree(InputElement element, bool shouldConsiderXYFocusKeyboardNavigation)
    {
        var isDirectionalRegion =
            shouldConsiderXYFocusKeyboardNavigation &&
            IsDirectionalRegion(element);

        return element.IsEffectivelyVisible &&
               element.IsEffectivelyEnabled &&
               // !FocusProperties.ShouldSkipFocusSubTree(element) &&
               (!shouldConsiderXYFocusKeyboardNavigation || isDirectionalRegion);
    }

    internal static bool IsCandidateParticipatingInScroll(InputElement candidate, InputElement? activeScroller)
    {
        return false;
        // if (activeScroller == null)
        //     return false;
        //
        // InputElement parent = candidate;
        // while (parent != null)
        // {
        //     if (parent is CUIElement element && element.IsScroller())
        //     {
        //         return parent == activeScroller;
        //     }
        //     parent = parent.Parent;
        // }
        // return false;
    }

    internal static bool IsCandidateChildOfAncestorScroller(InputElement candidate, InputElement? activeScroller)
    {
        if (activeScroller == null)
            return false;

        return false;
        // InputElement parent = activeScroller.Parent;
        // while (parent != null)
        // {
        //     if (parent is CUIElement element && element.IsScroller())
        //     {
        //         if (parent.IsAncestorOf(candidate))
        //             return true;
        //     }
        //     parent = parent.Parent;
        // }
        // return false;
    }

    internal static bool IsDirectionalRegion(InputElement? element)
    {
        if (element is null)
            return false;

        return GetKeyboardNavigationEnabled(element);
    }
    
    internal static bool IsOccluded(InputElement element, Rect elementBounds)
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

    internal static Rect? GetBoundsForRanking(InputElement element, bool ignoreClipping)
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

