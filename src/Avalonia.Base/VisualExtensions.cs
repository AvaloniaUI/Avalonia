using System;
using Avalonia.VisualTree;

namespace Avalonia
{
    /// <summary>
    /// Extension methods for <see cref="IVisual"/>.
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="point">The point in screen coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        public static Point PointToClient(this IVisual visual, PixelPoint point)
        {
            var root = visual.VisualRoot ??
                throw new ArgumentException("Control does not belong to a visual tree.", nameof(visual));
            var rootPoint = root.PointToClient(point);
            return root.TranslatePoint(rootPoint, visual)!.Value;
        }

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen coordinates.</returns>
        public static PixelPoint PointToScreen(this IVisual visual, Point point)
        {
            var root = visual.VisualRoot ??
                throw new ArgumentException("Control does not belong to a visual tree.", nameof(visual));
            var p = visual.TranslatePoint(point, root);
            return visual.VisualRoot.PointToScreen(p!.Value);
        }

        /// <summary>
        /// Returns a transform that transforms the visual's coordinates into the coordinates
        /// of the specified <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The visual whose coordinates are to be transformed.</param>
        /// <param name="to">The visual to translate the coordinates to.</param>
        /// <returns>
        /// A <see cref="Matrix"/> containing the transform or null if the visuals don't share a
        /// common ancestor.
        /// </returns>
        public static Matrix? TransformToVisual(this IVisual from, IVisual to)
        {
            var common = from.FindCommonVisualAncestor(to);

            if (common != null)
            {
                var thisOffset = GetOffsetFrom(common, from);
                var thatOffset = GetOffsetFrom(common, to);

                if (!thatOffset.TryInvert(out var thatOffsetInverted))
                {
                    return null;
                }

                return thatOffsetInverted * thisOffset;
            }

            return null;
        }

        /// <summary>
        /// Translates a point relative to this visual to coordinates that are relative to the specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="point">The point value, as relative to this visual.</param>
        /// <param name="relativeTo">The visual to translate the given point into.</param>
        /// <returns>
        /// A point value, now relative to the target visual rather than this source element, or null if the
        /// two elements have no common ancestor.
        /// </returns>
        public static Point? TranslatePoint(this IVisual visual, Point point, IVisual relativeTo)
        {
            var transform = visual.TransformToVisual(relativeTo);

            if (transform.HasValue)
            {
                return point.Transform(transform.Value);
            }

            return null;
        }

        /// <summary>
        /// Gets a transform from an ancestor to a descendent.
        /// </summary>
        /// <param name="ancestor">The ancestor visual.</param>
        /// <param name="visual">The visual.</param>
        /// <returns>The transform.</returns>
        private static Matrix GetOffsetFrom(IVisual ancestor, IVisual visual)
        {
            var result = Matrix.Identity;
            IVisual? v = visual;

            while (v != ancestor)
            {
                // this should be calculated BEFORE renderTransform
                if (v.HasMirrorTransform)
                {
                    var mirrorMatrix = new Matrix(-1.0, 0.0, 0.0, 1.0, v.Bounds.Width, 0);
                    result *= mirrorMatrix;
                }

                if (v.RenderTransform?.Value != null)
                {
                    var origin = v.RenderTransformOrigin.ToPixels(v.Bounds.Size);
                    var offset = Matrix.CreateTranslation(origin);
                    var renderTransform = (-offset) * v.RenderTransform.Value * (offset);

                    result *= renderTransform;
                }

                var topLeft = v.Bounds.TopLeft;

                if (topLeft != default)
                {
                    result *= Matrix.CreateTranslation(topLeft);
                }

                v = v.VisualParent;

                if (v == null)
                {
                    throw new ArgumentException("'visual' is not a descendant of 'ancestor'.");
                }
            }

            return result;
        }
    }
}
