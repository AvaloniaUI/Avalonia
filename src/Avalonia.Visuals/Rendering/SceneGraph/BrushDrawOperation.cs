// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : DrawOperation
    {
        public BrushDrawOperation(Rect bounds, Matrix transform, Pen pen)
            : base(bounds, transform, pen)
        {
        }

        /// <summary>
        /// Gets a collection of child scenes that are needed to draw visual brushes.
        /// </summary>
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }

        public static bool BrushEquals(IImmutableBrush left, IBrush right, double rightOpacity)
        {
            if (left == null && right == null)
            {
                return true;
            }
            else if (ReferenceEquals(left, right) && rightOpacity == 1)
            {
                return true;
            }
            else if (left is ImmutableImageBrush leftImage)
            {
                if (right is IImageBrush rightImage)
                {
                    return leftImage.AlignmentX == rightImage.AlignmentX &&
                        leftImage.AlignmentY == rightImage.AlignmentY &&
                        leftImage.DestinationRect == rightImage.DestinationRect &&
                        leftImage.Opacity == rightImage.Opacity * rightOpacity &&
                        Equals(leftImage.Source, rightImage.Source) &&
                        leftImage.SourceRect == rightImage.SourceRect &&
                        leftImage.Stretch == rightImage.Stretch &&
                        leftImage.TileMode == rightImage.TileMode;
                }
            }
            else if (left is ImmutableLinearGradientBrush leftLinear)
            {
                if (right is ILinearGradientBrush rightLinear)
                {
                    return leftLinear.EndPoint == rightLinear.EndPoint &&
                        Equal(leftLinear.GradientStops, rightLinear.GradientStops) &&
                        leftLinear.Opacity == rightLinear.Opacity * rightOpacity &&
                        leftLinear.SpreadMethod == rightLinear.SpreadMethod &&
                        leftLinear.StartPoint == rightLinear.StartPoint;
                }
            }
            else if (left is ImmutableRadialGradientBrush leftRadial)
            {
                if (right is IRadialGradientBrush rightRadial)
                {
                    return leftRadial.Center == rightRadial.Center &&
                        leftRadial.GradientOrigin == rightRadial.GradientOrigin &&
                        Equal(leftRadial.GradientStops, rightRadial.GradientStops) &&
                        leftRadial.Opacity == rightRadial.Opacity * rightOpacity &&
                        leftRadial.Radius == rightRadial.Radius &&
                        leftRadial.SpreadMethod == rightRadial.SpreadMethod;
                }
            }
            else if (left is ImmutableSolidColorBrush leftSolid)
            {
                if (right is ISolidColorBrush rightSolid)
                {
                    return leftSolid.Color == rightSolid.Color &&
                        leftSolid.Opacity == rightSolid.Opacity * rightOpacity;
                }
            }
            else if (left is ImmutableVisualBrush leftVisual)
            {
                if (right is IVisualBrush rightVisual)
                {
                    return leftVisual.AlignmentX == rightVisual.AlignmentX &&
                        leftVisual.AlignmentY == rightVisual.AlignmentY &&
                        leftVisual.DestinationRect == rightVisual.DestinationRect &&
                        leftVisual.Opacity == rightVisual.Opacity * rightOpacity &&
                        Equals(leftVisual.Visual, rightVisual.Visual) &&
                        leftVisual.SourceRect == rightVisual.SourceRect &&
                        leftVisual.Stretch == rightVisual.Stretch &&
                        leftVisual.TileMode == rightVisual.TileMode;
                }
            }

            return false;
        }

        public static bool PenEquals(Pen left, Pen right, double rightOpacity)
        {
            if (left == null && right == null)
            {
                return true;
            }
            else if (left == null || right == null)
            {
                return false;
            }
            else
            {
                return BrushEquals(left?.Brush as IImmutableBrush, right?.Brush, rightOpacity) &&
                    left.DashCap == right.DashCap &&
                    left.DashStyle == right.DashStyle &&
                    left.EndLineCap == right.EndLineCap &&
                    left.LineJoin == right.LineJoin &&
                    left.MiterLimit == right.MiterLimit &&
                    left.StartLineCap == right.StartLineCap &&
                    left.Thickness == right.Thickness;
            }
        }

        protected IImmutableBrush ToImmutable(IBrush brush, double opacity)
        {
            if (brush != null)
            {
                var immutable = brush.ToImmutable();
                return opacity == 1 ? immutable : immutable.WithOpacity(immutable.Opacity * opacity);
            }

            return null;
        }

        protected Pen ToImmutable(Pen pen, double opacity)
        {
            var brush = ToImmutable(pen?.Brush, opacity);
            return pen == null || ReferenceEquals(pen?.Brush, brush) ?
                pen :
                new Pen(
                    brush,
                    thickness: pen.Thickness,
                    dashStyle: pen.DashStyle,
                    dashCap: pen.DashCap,
                    startLineCap: pen.StartLineCap,
                    endLineCap: pen.EndLineCap,
                    lineJoin: pen.LineJoin,
                    miterLimit: pen.MiterLimit);
        }

        private static bool Equal(IList<GradientStop> left, IList<GradientStop> right)
        {
            if (left == null && right == null)
            {
                return true;
            }
            else if (left == null || right == null)
            {
                return false;
            }
            else if (left.Count != right.Count)
            {
                return false;
            }
            else
            {
                for (var i = 0; i < left.Count; ++i)
                {
                    if (left[i].Color != right[i].Color || left[i].Offset != right[i].Offset)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
