using System;
using System.Numerics;
using Avalonia.Utilities;

namespace Avalonia.Input.Navigation;

internal static class XYFocusAlgorithms
{
    private const double InShadowThreshold = 0.25;
    private const double InShadowThresholdForSecondaryAxis = 0.02;
    private const double ConeAngle = Math.PI / 4;

    private const double PrimaryAxisDistanceWeight = 15;
    private const double SecondaryAxisDistanceWeight = 1;
    private const double PercentInManifoldShadowWeight = 10000;
    private const double PercentInShadowWeight = 50;

    public static double GetScoreProximity(
        NavigationDirection direction,
        Rect bounds,
        Rect candidateBounds,
        double maxDistance,
        bool considerSecondaryAxis)
    {
        double score = 0;

        var primaryAxisDistance = CalculatePrimaryAxisDistance(direction, bounds, candidateBounds);
        var secondaryAxisDistance = CalculateSecondaryAxisDistance(direction, bounds, candidateBounds);

        if (primaryAxisDistance >= 0)
        {
            // We do not want to use the secondary axis if the candidate is within the shadow of the element
            (double, double) potential;
            (double, double) reference;

            if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
            {
                reference = (bounds.Top, bounds.Bottom);
                potential = (candidateBounds.Top, candidateBounds.Bottom);
            }
            else
            {
                reference = (bounds.Left, bounds.Right);
                potential = (candidateBounds.Left, candidateBounds.Right);
            }

            if (!considerSecondaryAxis || CalculatePercentInShadow(reference, potential) != 0)
            {
                secondaryAxisDistance = 0;
            }

            score = maxDistance - (primaryAxisDistance + secondaryAxisDistance);
        }

        return score;
    }
    
    public static double GetScoreProjection(
        NavigationDirection direction,
        Rect bounds,
        Rect candidateBounds,
        XYFocusManifolds manifolds,
        double maxDistance)
    {
        double score = 0;
        double primaryAxisDistance;
        double secondaryAxisDistance;
        double percentInManifoldShadow = 0;
        double percentInShadow;

        (double, double) potential;
        (double, double) reference;
        (double, double) currentManifold;

        if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
        {
            reference = (bounds.Top, bounds.Bottom);
            currentManifold = manifolds.HManifold;
            potential = (candidateBounds.Top, candidateBounds.Bottom);
        }
        else
        {
            reference = (bounds.Left, bounds.Right);
            currentManifold = manifolds.VManifold;
            potential = (candidateBounds.Left, candidateBounds.Right);
        }

        primaryAxisDistance = CalculatePrimaryAxisDistance(direction, bounds, candidateBounds);
        secondaryAxisDistance = CalculateSecondaryAxisDistance(direction, bounds, candidateBounds);

        if (primaryAxisDistance >= 0)
        {
            percentInShadow = CalculatePercentInShadow(reference, potential);

            if (percentInShadow >= InShadowThresholdForSecondaryAxis)
            {
                percentInManifoldShadow = CalculatePercentInShadow(currentManifold, potential);
                secondaryAxisDistance = maxDistance;
            }

            // The score needs to be a positive number so we make these distances positive numbers
            primaryAxisDistance = maxDistance - primaryAxisDistance;
            secondaryAxisDistance = maxDistance - secondaryAxisDistance;

            if (percentInShadow >= InShadowThreshold)
            {
                percentInShadow = 1;
                primaryAxisDistance = primaryAxisDistance * 2;
            }

            // Potential elements in the shadow get a multiplier to their final score
            score = CalculateScore(percentInShadow, primaryAxisDistance, secondaryAxisDistance,
                percentInManifoldShadow);
        }

        return score;
    }

    public static void UpdateManifolds(
        NavigationDirection direction,
        Rect bounds,
        Rect newFocusBounds,
        XYFocusManifolds manifolds)
    {
        var (vManifold, hManifold) = (manifolds.VManifold, manifolds.HManifold);

        if (vManifold.Right < 0)
        {
            vManifold = (bounds.Left, bounds.Right);
        }

        if (hManifold.Bottom < 0)
        {
            hManifold = (bounds.Top, bounds.Bottom);
        }

        if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
        {
            hManifold = (
                Math.Max(Math.Max(newFocusBounds.Top, bounds.Top), hManifold.Top),
                Math.Min(Math.Min(newFocusBounds.Bottom, bounds.Bottom), hManifold.Bottom));

            // It's possible to get into a situation where the newFocusedElement to the right / left has no overlap with the current edge.
            if (hManifold.Bottom <= hManifold.Top)
            {
                hManifold = (newFocusBounds.Top, newFocusBounds.Bottom);
            }

            vManifold = (newFocusBounds.Left, newFocusBounds.Right);
        }
        else if (direction == NavigationDirection.Up || direction == NavigationDirection.Down)
        {
            vManifold = (
                Math.Max(Math.Max(newFocusBounds.Left, bounds.Left), vManifold.Left),
                Math.Min(Math.Min(newFocusBounds.Right, bounds.Right), vManifold.Right));

            // It's possible to get into a situation where the newFocusedElement to the right / left has no overlap with the current edge.
            if (vManifold.Right <= vManifold.Left)
            {
                vManifold = (newFocusBounds.Left, newFocusBounds.Right);
            }

            hManifold = (newFocusBounds.Top, newFocusBounds.Bottom);
        }

        (manifolds.VManifold, manifolds.HManifold) = (vManifold, hManifold);
    }

    private static double CalculateScore(
        double percentInShadow,
        double primaryAxisDistance,
        double secondaryAxisDistance,
        double percentInManifoldShadow)
    {
        var score = (percentInShadow * PercentInShadowWeight) +
                    (primaryAxisDistance * PrimaryAxisDistanceWeight) +
                    (secondaryAxisDistance * SecondaryAxisDistanceWeight) +
                    (percentInManifoldShadow * PercentInManifoldShadowWeight);

        return score;
    }

    public static bool ShouldCandidateBeConsideredForRanking(
        Rect bounds,
        Rect candidateBounds,
        double maxDistance,
        NavigationDirection direction,
        Rect exclusionRect,
        bool ignoreCone)
    {
        // Consider a candidate only if:
        // 1. It doesn't have an empty rect as its bounds
        // 2. It doesn't contain the currently focused element
        // 3. Its bounds don't intersect with the rect we were asked to avoid looking into (Exclusion Rect)
        // 4. Its bounds aren't contained in the rect we were asked to avoid looking into (Exclusion Rect)
        if (candidateBounds.IsEmpty() ||
            candidateBounds.Contains(bounds) ||
            exclusionRect.Intersects(candidateBounds) ||
            exclusionRect.Contains(candidateBounds))
        {
            return false;
        }

        // We've decided to disable the use of the cone for vertical navigation.
        if (ignoreCone || direction == NavigationDirection.Down || direction == NavigationDirection.Up) { return true; }

        Vector originTop = new(0, (float)bounds.Top);
        Vector originBottom = new(0, (float)bounds.Bottom);

        var candidateAsPoints = new Vector[]
        {
            candidateBounds.TopLeft,
            candidateBounds.BottomLeft,
            candidateBounds.BottomRight,
            candidateBounds.TopRight
        };
        
        // We make the maxDistance twice the normal distance to ensure that all the elements are encapsulated inside the cone. This
        // also aids in scenarios where the original max distance is still less than one of the points (due to the angles)
        maxDistance = maxDistance * 2;

        Span<Vector> cone = stackalloc Vector[4];
        // Note: our y-axis is inverted
        if (direction == NavigationDirection.Left)
        {
            // We want to start the origin one pixel to the left to cover overlapping scenarios where the end of a candidate element 
            // could be overlapping with the origin (before the shift)
            originTop = new Vector(bounds.Left - 1, originTop.Y);
            originBottom = new Vector(bounds.Left - 1, originBottom.Y);

            // We have two angles. Find a point (for each angle) on the line and rotate based on the direction
            var rotation = Math.PI; // 180 degrees
            var sides = new Vector[]
            {
                new(
                    (originTop.X + maxDistance * Math.Cos(rotation + ConeAngle)),
                    (originTop.Y + maxDistance * Math.Sin(rotation + ConeAngle))),
                new(
                    (originBottom.X + maxDistance * Math.Cos(rotation - ConeAngle)),
                    (originBottom.Y + maxDistance * Math.Sin(rotation - ConeAngle)))
            };

            // Order points in counterclockwise direction
            cone[0] = originTop;
            cone[1] = sides[0];
            cone[2] = sides[1];
            cone[3] = originBottom;
        }
        else if (direction == NavigationDirection.Right)
        {
            // We want to start the origin one pixel to the right to cover overlapping scenarios where the end of a candidate element 
            // could be overlapping with the origin (before the shift)
            originTop = new Vector(bounds.Right + 1, originTop.Y);
            originBottom = new Vector(bounds.Right + 1, originBottom.Y);

            // We have two angles. Find a point (for each angle) on the line and rotate based on the direction
            double rotation = 0;
            var sides = new Vector[]
            {
                new(
                    (originTop.X + maxDistance * Math.Cos(rotation + ConeAngle)),
                    (originTop.Y + maxDistance * Math.Sin(rotation + ConeAngle))),
                new(
                    (originBottom.X + maxDistance * Math.Cos(rotation - ConeAngle)),
                    (originBottom.Y + maxDistance * Math.Sin(rotation - ConeAngle)))
            };

            // Order points in counterclockwise direction
            cone[0] = originBottom;
            cone[1] = sides[0];
            cone[2] = sides[1];
            cone[3] = originTop;
        }

        // There are three scenarios we should check that will allow us to know whether we should consider the candidate element.
        // 1) The candidate element and the vision cone intersect
        // 2) The candidate element is completely inside the vision cone
        // 3) The vision cone is completely inside the bounds of the candidate element (unlikely)

        return MathUtilities.DoPolygonsIntersect(4, cone, 4, candidateAsPoints)
               || MathUtilities.IsEntirelyContained(4, candidateAsPoints, 4, cone)
               || MathUtilities.IsEntirelyContained(4, cone, 4, candidateAsPoints);
    }
    
    private static double CalculatePrimaryAxisDistance(
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

    private static double CalculateSecondaryAxisDistance(
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
    /// In other words, this method calculates percentage overlap of two elements ranges (top+bottom or left+right).
    private static double CalculatePercentInShadow(
        (double first, double second) referenceManifold,
        (double first, double second) potentialManifold)
    {
        if (referenceManifold.first > potentialManifold.second || referenceManifold.second <= potentialManifold.first)
            // Potential is not in the reference's shadow.
            return 0;

        var shadow = Math.Min(referenceManifold.second, potentialManifold.second) -
                     Math.Max(referenceManifold.first, potentialManifold.first);
        shadow = Math.Abs(shadow);

        var potentialEdgeLength = Math.Abs(potentialManifold.second - potentialManifold.first);
        var referenceEdgeLength = Math.Abs(referenceManifold.second - referenceManifold.first);

        var comparisonEdgeLength = referenceEdgeLength;

        if (comparisonEdgeLength >= potentialEdgeLength) comparisonEdgeLength = potentialEdgeLength;

        double percentInShadow = 1;

        if (comparisonEdgeLength != 0) percentInShadow = Math.Min(shadow / comparisonEdgeLength, 1.0);

        return percentInShadow;
    }

    internal class XYFocusManifolds
    {
        public (double Left, double Right) VManifold { get; set; }
        public (double Top, double Bottom) HManifold { get; set; }

        public XYFocusManifolds()
        {
            Reset();
        }

        public void Reset()
        {
            VManifold = (-1.0, -1.0);
            HManifold = (-1.0, -1.0);
        }
    }
}
