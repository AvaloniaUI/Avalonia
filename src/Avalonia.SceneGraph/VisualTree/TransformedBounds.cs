// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Holds information about the bounds of a control, together with a transform and a clip.
    /// </summary>
    public struct TransformedBounds
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformedBounds"/> struct.
        /// </summary>
        /// <param name="bounds">The control's bounds.</param>
        /// <param name="clip">The control's clip rectangle.</param>
        /// <param name="transform">The control's transform.</param>
        public TransformedBounds(Rect bounds, Rect clip, Matrix transform)
        {
            Bounds = bounds;
            Clip = clip;
            Transform = transform;
        }

        /// <summary>
        /// Gets the control's bounds.
        /// </summary>
        public Rect Bounds { get; }

        /// <summary>
        /// Gets the control's clip rectangle.
        /// </summary>
        public Rect Clip { get; }

        /// <summary>
        /// Gets the control's transform.
        /// </summary>
        public Matrix Transform { get; }

        public Geometry GetTransformedBoundsGeometry()
        {
            StreamGeometry geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.SetFillRule(FillRule.EvenOdd);
                context.BeginFigure(Bounds.TopLeft * Transform, true);
                context.LineTo(Bounds.TopRight * Transform);
                context.LineTo(Bounds.BottomRight * Transform);
                context.LineTo(Bounds.BottomLeft * Transform);
                context.LineTo(Bounds.TopLeft * Transform);
                context.EndFigure(true);
            }
            return geometry;
        }
    }
}
