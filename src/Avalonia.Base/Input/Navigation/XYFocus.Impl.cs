using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Input.Navigation;
using Avalonia.Media;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Input;

internal record XYFocusParams(InputElement Element, Rect Bounds)
{
    public double Score { get; set; }
}

public partial class XYFocus
{
    private XYFocusAlgorithms.XYFocusManifolds mManifolds = new();
    private XYFocusAlgorithms mHeuristic = new();
    private List<long> mExploredList = new();

    internal XYFocusAlgorithms.XYFocusManifolds ResetManifolds()
    {
        mManifolds.Reset();
        return mManifolds;
    }

    internal void SetManifolds(XYFocusAlgorithms.XYFocusManifolds manifolds)
    {
        mManifolds.VManifold = manifolds.VManifold;
        mManifolds.HManifold = manifolds.HManifold;
    }

    internal void ClearCache()
    {
        mExploredList.Clear();
    }

    internal InputElement? GetNextFocusableElement(
        NavigationDirection direction,
        InputElement? element,
        InputElement? engagedControl,
        bool updateManifolds,
        XYFocusOptions xyFocusOptions)
    {
        if (element == null) return null;

        long hash = 0;
        if (mExploredList.Count > 0)
        {
            hash = ExploredListHash(direction, element, engagedControl, xyFocusOptions);
            if (mExploredList.Contains(hash))
            {
                CacheHitTrace(direction);
                return null;
            }
        }

        var root = (InputElement)element.GetVisualRoot()!;
        var isRightToLeft = element.FlowDirection == FlowDirection.RightToLeft;
        var mode = GetStrategy(element, direction, xyFocusOptions.NavigationStrategyOverride);

        Rect rootBounds;

        var focusedElementBounds = xyFocusOptions.FocusedElementBoundsOverride ?? GetBoundsForRanking(element, true);
        if (focusedElementBounds is null)
        {
            return null;
        }

        var nextFocusableElement = GetDirectionOverride(element, xyFocusOptions.SearchRoot, direction, true);

        if (nextFocusableElement != null)
        {
            return nextFocusableElement;
        }

        var activeScroller = GetActiveScrollerForScroll(direction, element);
        var isProcessingInputForScroll = (activeScroller != null);

        if (xyFocusOptions.FocusHintRectangle != null)
        {
            focusedElementBounds = xyFocusOptions.FocusHintRectangle.Value;
            element = null;
        }

        if (engagedControl != null)
        {
            rootBounds = GetBoundsForRanking(engagedControl, xyFocusOptions.IgnoreClipping) ?? root.Bounds;
        }
        else if (xyFocusOptions.SearchRoot != null)
        {
            rootBounds = GetBoundsForRanking(xyFocusOptions.SearchRoot, xyFocusOptions.IgnoreClipping) ?? root.Bounds;
        }
        else
        {
            rootBounds = GetBoundsForRanking(root, xyFocusOptions.IgnoreClipping) ?? root.Bounds;
        }

        var candidateList = GetAllValidFocusableChildren(root, direction, element, engagedControl,
            xyFocusOptions.SearchRoot, activeScroller, xyFocusOptions.IgnoreClipping,
            xyFocusOptions.ShouldConsiderXYFocusKeyboardNavigation);

        if (candidateList.Count > 0)
        {
            var maxRootBoundsDistance =
                Math.Max(rootBounds.Right - rootBounds.Left, rootBounds.Bottom - rootBounds.Top);
            maxRootBoundsDistance = Math.Max(maxRootBoundsDistance,
                GetMaxRootBoundsDistance(candidateList, focusedElementBounds.Value, direction,
                    xyFocusOptions.IgnoreClipping));

            RankElements(ref candidateList, direction, focusedElementBounds.Value, maxRootBoundsDistance, mode,
                xyFocusOptions.ExclusionRect, xyFocusOptions.IgnoreClipping, xyFocusOptions.IgnoreCone);

#if XYFOCUS_DBG
                foreach (var it in candidateList)
                {
                    RAWTRACE(TraceAlways, $"Candidate: {it.Element} {it.Bounds.Left},{it.Bounds.Top} {it.Bounds.Right},{it.Bounds.Bottom} rank {it.Score}");
                }
#endif

            var ignoreOcclusivity = xyFocusOptions.IgnoreOcclusivity || isProcessingInputForScroll;

            // Choose the best candidate, after testing for occlusivity, if we're currently scrolling, the test has been done already, skip it.
            nextFocusableElement = ChooseBestFocusableElementFromList(candidateList, direction, focusedElementBounds.Value,
                xyFocusOptions.IgnoreClipping, ignoreOcclusivity, isRightToLeft,
                xyFocusOptions.UpdateManifold && updateManifolds);
            if (element is not null)
            {
                nextFocusableElement = TryXYFocusBubble(element, nextFocusableElement, xyFocusOptions.SearchRoot, direction);
            }
        }

        // Store the result in the explored list if the candidate is null.
        if (nextFocusableElement == null)
        {
            if (hash == 0)
            {
                hash = ExploredListHash(direction, element, engagedControl, xyFocusOptions);
            }

            mExploredList.Add(hash);
        }

        return nextFocusableElement;
    }

    private InputElement? ChooseBestFocusableElementFromList(
        List<XYFocusParams> scoreList,
        NavigationDirection direction,
        Rect bounds,
        bool ignoreClipping,
        bool ignoreOcclusivity,
        bool isRightToLeft,
        bool updateManifolds)
    {
        InputElement? bestElement = null;

        scoreList.Sort((elementA, elementB) =>
        {
            if (elementA.Element == elementB.Element)
            {
                return 0;
            }

            var compared = elementB.Score.CompareTo(elementA.Score);
            if (compared == 0)
            {
                var firstBounds = elementA.Bounds;
                var secondBounds = elementB.Bounds;

                if (firstBounds == secondBounds)
                {
                    return 0;
                }
                else if (direction == NavigationDirection.Up || direction == NavigationDirection.Down)
                {
                    if (isRightToLeft)
                    {
                        return secondBounds.Left.CompareTo(firstBounds.Left);
                    }

                    return firstBounds.Left.CompareTo(secondBounds.Left);
                }
                else
                {
                    return firstBounds.Top.CompareTo(secondBounds.Top);
                }
            }

            return compared;
        });

        foreach (var param in scoreList)
        {
            if (param.Score <= 0)
            {
                break;
            }

            var boundsForOccTesting =
                ignoreClipping ? GetBoundsForRanking(param.Element, false)!.Value : param.Bounds;

            // Don't check for occlusivity if we've already covered occlusivity scenarios for scrollable content or have been asked
            // to ignore occlusivity by the caller.
            if (Math.Abs(param.Bounds.X - double.MaxValue) > MathUtilities.DoubleEpsilon &&
                (ignoreOcclusivity || !IsOccluded(param.Element, boundsForOccTesting)))
            {
                bestElement = param.Element;

                if (updateManifolds)
                {
                    // Update the manifolds with the newly selected focus
                    mHeuristic.UpdateManifolds(direction, bounds, param.Bounds, mManifolds);
                }

                break;
            }
        }

        return bestElement;
    }

    private void UpdateManifolds(
        NavigationDirection direction,
        Rect elementBounds,
        InputElement candidate,
        bool ignoreClipping)
    {
        var candidateBounds = GetBoundsForRanking(candidate, ignoreClipping)!.Value;
        mHeuristic.UpdateManifolds(direction, elementBounds, candidateBounds, mManifolds);
    }

    private List<XYFocusParams> GetAllValidFocusableChildren(
        InputElement startRoot,
        NavigationDirection direction,
        InputElement? currentElement,
        InputElement? engagedControl,
        InputElement? searchScope,
        InputElement? activeScroller,
        bool ignoreClipping,
        bool shouldConsiderXYFocusKeyboardNavigation)
    {
        var rootForTreeWalk = startRoot;
        var candidateList = new List<XYFocusParams>();
        FocusWalkTraceBegin(direction);

        // If asked to scope the search within the given container, honor it without any exceptions
        if (searchScope != null)
        {
            rootForTreeWalk = searchScope;
        }

        if (engagedControl == null)
        {
            candidateList = FindElements(rootForTreeWalk, currentElement, activeScroller, ignoreClipping,
                shouldConsiderXYFocusKeyboardNavigation);
        }
        else
        {
            // Only run through this when you are an engaged element. Being an engaged element means that you should only
            // look at the children of the engaged element and any children of popups that were opened during engagement
            candidateList = FindElements(engagedControl, currentElement, activeScroller, ignoreClipping,
                shouldConsiderXYFocusKeyboardNavigation);
            
            // Iterate through the popups and add their children to the list
            // TODO: AVALONIA
            // var popupChildrenDuringEngagement = CPopupRoot.GetPopupChildrenOpenedDuringEngagement(engagedControl);
            // foreach (var popup in popupChildrenDuringEngagement)
            // {
            //     var subCandidateList = FindElements(popup, currentElement, activeScroller,
            //         ignoreClipping, shouldConsiderXYFocusKeyboardNavigation);
            //     candidateList.AddRange(subCandidateList);
            // }
            
            if (currentElement != engagedControl
                && GetBoundsForRanking(engagedControl, ignoreClipping) is {} bounds)
            {
                candidateList.Add(new XYFocusParams(engagedControl, bounds));
            }
        }

        // TraceXYFocusWalkEnd();
        return candidateList;
    }

    private void RankElements(
        ref List<XYFocusParams> candidateList,
        NavigationDirection direction,
        Rect bounds,
        double maxRootBoundsDistance,
        XYFocusNavigationStrategy mode,
        Rect? exclusionRect,
        bool ignoreClipping,
        bool ignoreCone)
    {
        var exclusionBounds = new Rect();
        if (exclusionRect != null)
        {
            exclusionBounds = exclusionRect.Value;
        }

        foreach (var candidate in candidateList)
        {
            var candidateBounds = candidate.Bounds;

            if (!(exclusionBounds.Intersects(candidateBounds) || exclusionBounds.Contains(candidateBounds)))
            {
                if (mode == XYFocusNavigationStrategy.Projection &&
                    mHeuristic.ShouldCandidateBeConsideredForRanking(bounds, candidateBounds, maxRootBoundsDistance,
                        direction, exclusionBounds, ignoreCone))
                {
                    candidate.Score = mHeuristic.GetScore(direction, bounds, candidateBounds, mManifolds, maxRootBoundsDistance);
                }
                else if (mode == XYFocusNavigationStrategy.NavigationDirectionDistance ||
                         mode == XYFocusNavigationStrategy.RectilinearDistance)
                {
                    candidate.Score = XYFocusAlgorithms.GetScoreGlobal(direction, bounds, candidateBounds,
                        maxRootBoundsDistance, mode == XYFocusNavigationStrategy.RectilinearDistance);
                }
            }
        }
    }

    private double GetMaxRootBoundsDistance(
        List<XYFocusParams> list,
        Rect bounds,
        NavigationDirection direction,
        bool ignoreClipping)
    {
        var maxElement = list[0];
        var maxValue = double.MinValue;

        foreach (var param in list)
        {
            var candidateBounds = param.Bounds;
            var value = direction switch
            {
                NavigationDirection.Left => candidateBounds.Left,
                NavigationDirection.Right => candidateBounds.Right,
                NavigationDirection.Up => candidateBounds.Top,
                NavigationDirection.Down => candidateBounds.Bottom,
                _ => 0
            };

            if (value > maxValue)
            {
                maxValue = value;
                maxElement = param;
            }
        }

        var maxBounds = maxElement.Bounds;
        return direction switch
        {
            NavigationDirection.Left => Math.Abs(maxBounds.Right - bounds.Left),
            NavigationDirection.Right => Math.Abs(bounds.Right - maxBounds.Left),
            NavigationDirection.Up => Math.Abs(bounds.Bottom - maxBounds.Top),
            NavigationDirection.Down => Math.Abs(maxBounds.Bottom - bounds.Top),
            _ => 0,
        };
    }

    private InputElement? GetActiveScrollerForScroll(
        NavigationDirection direction,
        InputElement focusedElement)
    {
        // InputElement parent = null;
        // var textElement = focusedElement as TextElement;
        // if (textElement != null)
        // {
        //     parent = textElement.GetContainingFrameworkElement();
        // }
        // else
        // {
        //     parent = focusedElement;
        // }
        //
        // while (parent != null)
        // {
        //     var element = parent;
        //     if (element != null && element.IsScroller())
        //     {
        //         bool isHorizontallyScrollable = false;
        //         bool isVerticallyScrollable = false;
        //         FocusProperties.IsScrollable(element, out isHorizontallyScrollable, out isVerticallyScrollable);
        //
        //         bool isHorizontallyScrollableForDirection =
        //             IsHorizontalNavigationDirection(direction) && isHorizontallyScrollable;
        //         bool isVerticallyScrollableForDirection =
        //             IsVerticalNavigationDirection(direction) && isVerticallyScrollable;
        //
        //         Debug.Assert(!(isHorizontallyScrollableForDirection && isVerticallyScrollableForDirection));
        //
        //         if (isHorizontallyScrollableForDirection || isVerticallyScrollableForDirection)
        //         {
        //             return element;
        //         }
        //     }
        //
        //     parent = (parent as Visual)?.VisualParent as InputElement;
        // }

        return null;
    }

    bool IsHorizontalNavigationDirection(NavigationDirection direction)
    {
        return direction == NavigationDirection.Left || direction == NavigationDirection.Right;
    }

    bool IsVerticalNavigationDirection(NavigationDirection direction)
    {
        return direction == NavigationDirection.Up || direction == NavigationDirection.Down;
    }

    void SetPrimaryAxisDistanceWeight(int primaryAxisDistanceWeight) =>
        mHeuristic.PrimaryAxisDistanceWeight = primaryAxisDistanceWeight;

    void SetSecondaryAxisDistanceWeight(int secondaryAxisDistanceWeight) =>
        mHeuristic.SecondaryAxisDistanceWeight = secondaryAxisDistanceWeight;

    void SetPercentInManifoldShadowWeight(int percentInManifoldShadowWeight) =>
        mHeuristic.PercentInManifoldShadowWeight = percentInManifoldShadowWeight;

    void SetPercentInShadowWeight(int percentInShadowWeight) =>
        mHeuristic.PercentInShadowWeight = percentInShadowWeight;

    private static int ExploredListHash(
        NavigationDirection direction,
        InputElement? element,
        InputElement? engagedControl,
        XYFocusOptions xyFocusOptions)
    {
        return (direction, element, engagedControl, xyFocusOptions).GetHashCode();
    }

    void FocusWalkTraceBegin(NavigationDirection direction)
    {
        // switch (direction)
        // {
        //     case NavigationDirection.Next:
        //         TraceXYFocusWalkBegin("Next");
        //         break;
        //     case NavigationDirection.Previous:
        //         TraceXYFocusWalkBegin("Previous");
        //         break;
        //     case NavigationDirection.Up:
        //         TraceXYFocusWalkBegin("Up");
        //         break;
        //     case NavigationDirection.Down:
        //         TraceXYFocusWalkBegin("Down");
        //         break;
        //     case NavigationDirection.Left:
        //         TraceXYFocusWalkBegin("Left");
        //         break;
        //     case NavigationDirection.Right:
        //         TraceXYFocusWalkBegin("Right");
        //         break;
        //     default:
        //         TraceXYFocusWalkBegin("Invalid");
        //         break;
        // }
    }

    void CacheHitTrace(NavigationDirection direction)
    {
        // switch (direction)
        // {
        //     case NavigationDirection.Next:
        //         TraceXYFocusCandidateCacheHit("Next");
        //         break;
        //     case NavigationDirection.Previous:
        //         TraceXYFocusCandidateCacheHit("Previous");
        //         break;
        //     case NavigationDirection.Up:
        //         TraceXYFocusCandidateCacheHit("Up");
        //         break;
        //     case NavigationDirection.Down:
        //         TraceXYFocusCandidateCacheHit("Down");
        //         break;
        //     case NavigationDirection.Left:
        //         TraceXYFocusCandidateCacheHit("Left");
        //         break;
        //     case NavigationDirection.Right:
        //         TraceXYFocusCandidateCacheHit("Right");
        //         break;
        //     default:
        //         TraceXYFocusCandidateCacheHit("Invalid");
        //         break;
        // }
    }
}
