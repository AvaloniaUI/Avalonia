using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Navigation;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Input;

public partial class XYFocus
{
    private static InputElement? GetDirectionOverride(
        InputElement element,
        InputElement? searchRoot,
        NavigationDirection direction,
        bool ignoreFocusabililty = false)
    {
        var index = GetXYFocusPropertyIndex(element, direction);

        if (index != null)
        {
            var overrideElement = element.GetValue(index) as InputElement;

            if (overrideElement != null)
            {
                if ((!ignoreFocusabililty && !FocusManager.CanFocus(overrideElement)))
                    return null;

                // If an override was specified but it is located outside the searchRoot, don't use it as the candidate.
                if (searchRoot != null &&
                    !searchRoot.IsVisualAncestorOf(overrideElement))
                    return null;

                return overrideElement;
            }
        }

        return null;
    }

    private static InputElement? TryXYFocusBubble(
        InputElement element,
        InputElement? candidate,
        InputElement? searchRoot,
        NavigationDirection direction)
    {
        if (candidate == null)
            return null;

        var nextFocusableElement = candidate;
        var directionOverrideRoot = GetDirectionOverrideRoot(element, searchRoot, direction);

        if (directionOverrideRoot != null)
        {
            var isAncestor = directionOverrideRoot.IsVisualAncestorOf(candidate);
            if (!isAncestor)
            {
                nextFocusableElement = GetDirectionOverride(directionOverrideRoot, searchRoot, direction)
                                       ?? nextFocusableElement;
            }
        }

        return nextFocusableElement;
    }

    private static InputElement? GetDirectionOverrideRoot(
        InputElement element,
        InputElement? searchRoot,
        NavigationDirection direction)
    {
        var root = element;

        while (root != null && GetDirectionOverride(root, searchRoot, direction) == null)
        {
            root = root.GetVisualParent() as InputElement;
        }

        return root;
    }

    private static XYFocusNavigationStrategy GetStrategy(
        InputElement element,
        NavigationDirection direction,
        XYFocusNavigationStrategy? navigationStrategyOverride)
    {
        var isAutoOverride = navigationStrategyOverride == XYFocusNavigationStrategy.Auto;

        if (navigationStrategyOverride.HasValue && !isAutoOverride)
        {
            // We can cast just by offsetting values because we have ensured that the XYFocusStrategy enums offset as expected
            return (XYFocusNavigationStrategy)(int)(navigationStrategyOverride.Value - 1);
        }
        else if (isAutoOverride && element.GetVisualParent() is InputElement parent)
        {
            // Skip the element if we have an auto override and look at its parent's strategy
            element = parent;
        }
        
        var index = GetXYFocusNavigationStrategyPropertyIndex(element, direction);
        if (index is not null)
        {
            var current = element;
            while (current != null && current.GetValue(index) is XYFocusNavigationStrategy mode)
            {
                if (mode != XYFocusNavigationStrategy.Auto)
                {
                    return mode;
                }

                current = current.GetVisualParent() as InputElement;
            }
        }

        return XYFocusNavigationStrategy.Projection;
    }

    private static AvaloniaProperty? GetXYFocusPropertyIndex(
        InputElement element,
        NavigationDirection direction)
    {
        if (element.FlowDirection == FlowDirection.RightToLeft)
        {
            if (direction == NavigationDirection.Left) direction = NavigationDirection.Right;
            else if (direction == NavigationDirection.Right) direction = NavigationDirection.Left;
        }

        switch (direction)
        {
            case NavigationDirection.Left:
                return LeftProperty;
            case NavigationDirection.Right:
                return RightProperty;
            case NavigationDirection.Up:
                return UpProperty;
            case NavigationDirection.Down:
                return DownProperty;
        }

        return null;
    }

    private static AvaloniaProperty? GetXYFocusNavigationStrategyPropertyIndex(
        InputElement element,
        NavigationDirection direction)
    {
        if (element.FlowDirection == FlowDirection.RightToLeft)
        {
            if (direction == NavigationDirection.Left) direction = NavigationDirection.Right;
            else if (direction == NavigationDirection.Right) direction = NavigationDirection.Left;
        }

        switch (direction)
        {
            case NavigationDirection.Left:
                return LeftNavigationStrategyProperty;
            case NavigationDirection.Right:
                return RightNavigationStrategyProperty;
            case NavigationDirection.Up:
                return UpNavigationStrategyProperty;
            case NavigationDirection.Down:
                return DownNavigationStrategyProperty;
        }

        return null;
    }
}
