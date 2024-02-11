using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Primitives;
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
    internal XYFocus()
    {
        
    }
    
    private XYFocusAlgorithms.XYFocusManifolds mManifolds = new();
    private PooledList<XYFocusParams> _pooledCandidates = new();

    private static readonly XYFocus _instance = new();
    
    internal XYFocusAlgorithms.XYFocusManifolds ResetManifolds()
    {
        mManifolds.Reset();
        return mManifolds;
    }

    internal void SetManifoldsFromBounds(Rect bounds)
    {
        mManifolds.VManifold = (bounds.Left, bounds.Right);
        mManifolds.HManifold = (bounds.Top, bounds.Bottom);
    }

    internal void UpdateManifolds(
        NavigationDirection direction,
        Rect elementBounds,
        InputElement candidate,
        bool ignoreClipping)
    {
        var candidateBounds = GetBoundsForRanking(candidate, ignoreClipping)!.Value;
        XYFocusAlgorithms.UpdateManifolds(direction, elementBounds, candidateBounds, mManifolds);
    }

    internal static InputElement? TryDirectionalFocus(
        NavigationDirection direction,
        IInputElement element,
        IInputElement? owner,
        InputElement? engagedControl,
        KeyDeviceType? keyDeviceType)
    {
        /*
         * UWP/WinUI Behavior is a bit different with handling of manifolds.
         * In WinUI SetManifolds is called with Hint boundaries of the currently focused element.
         * And once again UpdateManifolds is called after successfully completed focus operation.
         * Guaranteeing that Projection navigation algorithm (the only one that actually respects manifolds)
         * will respect manifolds these manifolds with higher coefficient.
         * Note, it's not quite clear from WinUI source code in which scenario
         * these manifolds would differ from currently focused elements boundaries.
         * The only possible situation is when XYFocusOptions.FocusedElementBounds has custom value,
         * and current element boundaries are ignored. Possibly, it is used by their internal testing (not open-sourced)?
         * So, for Avalonia I have added this GetNextFocusableElement method that simplifies algorithm a little,
         * by forcing current elements boundaries to the manifolds always.
         *
         * Also, with using static GetNextFocusableElement and self-managed manifolds, we don't need XYFocus instance object anymore.
         *
         * This method also hides initialization of some XYFocusOptions properties.
         * Keep in mind, UWP gives much more flexibility with focus than Avalonia currently does, so some properties are ignored.
         */

        if (!(element is InputElement inputElement))
        {
            // TODO: handle non-Visual IInputElement implementations, like TextElement, when we support that.
            return null;
        }

        if (!XYFocusHelpers.IsAllowedXYNavigationMode(inputElement, keyDeviceType))
        {
            return null;
        }

        if (!(GetBoundsForRanking(inputElement, true) is { } bounds))
        {
            return null;
        }

        _instance.SetManifoldsFromBounds(bounds);

        return _instance.GetNextFocusableElement(direction, inputElement, engagedControl, true, new XYFocusOptions
        {
            KeyDeviceType = keyDeviceType,
            FocusedElementBounds = bounds,
            UpdateManifold = true,
            SearchRoot = owner as InputElement ?? inputElement.GetVisualRoot() as InputElement
        });
    }

    internal InputElement? GetNextFocusableElement(
        NavigationDirection direction,
        InputElement? element,
        InputElement? engagedControl,
        bool updateManifolds,
        XYFocusOptions xyFocusOptions)
    {
        if (element == null) return null;

        var root = (InputElement)element.GetVisualRoot()!;
        var isRightToLeft = element.FlowDirection == FlowDirection.RightToLeft;
        var mode = GetStrategy(element, direction, xyFocusOptions.NavigationStrategyOverride);

        Rect rootBounds;

        var focusedElementBounds = xyFocusOptions.FocusedElementBounds ??
                                   throw new InvalidOperationException("FocusedElementBounds needs to be set");

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

        var candidateList = _pooledCandidates;
        try
        {
            GetAllValidFocusableChildren(candidateList, root, direction, element, engagedControl,
                xyFocusOptions.SearchRoot, activeScroller, xyFocusOptions.IgnoreClipping,
                xyFocusOptions.KeyDeviceType);

            if (candidateList.Count > 0)
            {
                var maxRootBoundsDistance =
                    Math.Max(rootBounds.Right - rootBounds.Left, rootBounds.Bottom - rootBounds.Top);
                maxRootBoundsDistance = Math.Max(maxRootBoundsDistance,
                    GetMaxRootBoundsDistance(candidateList, focusedElementBounds, direction,
                        xyFocusOptions.IgnoreClipping));

                RankElements(candidateList, direction, focusedElementBounds, maxRootBoundsDistance, mode,
                    xyFocusOptions.ExclusionRect, xyFocusOptions.IgnoreClipping, xyFocusOptions.IgnoreCone);

                var ignoreOcclusivity = xyFocusOptions.IgnoreOcclusivity || isProcessingInputForScroll;

                // Choose the best candidate, after testing for occlusivity, if we're currently scrolling, the test has been done already, skip it.
                nextFocusableElement = ChooseBestFocusableElementFromList(candidateList, direction,
                    focusedElementBounds,
                    xyFocusOptions.IgnoreClipping, ignoreOcclusivity, isRightToLeft,
                    xyFocusOptions.UpdateManifold && updateManifolds);
                if (element is not null)
                {
                    nextFocusableElement = TryXYFocusBubble(element, nextFocusableElement, xyFocusOptions.SearchRoot,
                        direction);
                }
            }
        }
        finally
        {
            _pooledCandidates.Clear();
        }

        return nextFocusableElement;
    }

    private InputElement? ChooseBestFocusableElementFromList(
        PooledList<XYFocusParams> scoreList,
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
            if (elementA!.Element == elementB!.Element)
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
                    XYFocusAlgorithms.UpdateManifolds(direction, bounds, param.Bounds, mManifolds);
                }

                break;
            }
        }

        return bestElement;
    }

    private void GetAllValidFocusableChildren(
        PooledList<XYFocusParams> candidateList,
        InputElement startRoot,
        NavigationDirection direction,
        InputElement? currentElement,
        InputElement? engagedControl,
        InputElement? searchScope,
        InputElement? activeScroller,
        bool ignoreClipping,
        KeyDeviceType? inputKeyDeviceType)
    {
        var rootForTreeWalk = startRoot;

        // If asked to scope the search within the given container, honor it without any exceptions
        if (searchScope != null)
        {
            rootForTreeWalk = searchScope;
        }

        if (engagedControl == null)
        {
            FindElements(candidateList, rootForTreeWalk, currentElement, activeScroller, ignoreClipping,
                inputKeyDeviceType);
        }
        else
        {
            // Only run through this when you are an engaged element. Being an engaged element means that you should only
            // look at the children of the engaged element and any children of popups that were opened during engagement
            FindElements(candidateList, engagedControl, currentElement, activeScroller, ignoreClipping,
                inputKeyDeviceType);
            
            // Iterate through the popups and add their children to the list
            // TODO: Avalonia, missing Popup API
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
    }

    private void RankElements(
        IList<XYFocusParams> candidateList,
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
                    XYFocusAlgorithms.ShouldCandidateBeConsideredForRanking(bounds, candidateBounds, maxRootBoundsDistance,
                        direction, exclusionBounds, ignoreCone))
                {
                    candidate.Score = XYFocusAlgorithms.GetScoreProjection(direction, bounds, candidateBounds, mManifolds, maxRootBoundsDistance);
                }
                else if (mode == XYFocusNavigationStrategy.NavigationDirectionDistance ||
                         mode == XYFocusNavigationStrategy.RectilinearDistance)
                {
                    candidate.Score = XYFocusAlgorithms.GetScoreProximity(direction, bounds, candidateBounds,
                        maxRootBoundsDistance, mode == XYFocusNavigationStrategy.RectilinearDistance);
                }
            }
        }
    }

    private double GetMaxRootBoundsDistance(
        IList<XYFocusParams> list,
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
        InputElement? parent = null;
        // var textElement = focusedElement as TextElement;
        // if (textElement != null)
        // {
        //     parent = textElement.GetContainingFrameworkElement();
        // }
        // else
        {
            parent = focusedElement;
        }
        
        while (parent != null)
        {
            var element = parent;
            if (element is IInternalScroller scrollable)
            {
                var isHorizontallyScrollable = scrollable.CanHorizontallyScroll;
                var isVerticallyScrollable = scrollable.CanVerticallyScroll;
        
                var isHorizontallyScrollableForDirection =
                    direction is NavigationDirection.Left or NavigationDirection.Right
                    && isHorizontallyScrollable;
                var isVerticallyScrollableForDirection =
                    direction is NavigationDirection.Up or NavigationDirection.Down
                    && isVerticallyScrollable;
        
                Debug.Assert(!(isHorizontallyScrollableForDirection && isVerticallyScrollableForDirection));
        
                if (isHorizontallyScrollableForDirection || isVerticallyScrollableForDirection)
                {
                    return element;
                }
            }
        
            parent = parent.VisualParent as InputElement;
        }

        return null;
    }
}
