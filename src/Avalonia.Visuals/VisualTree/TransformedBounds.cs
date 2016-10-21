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

        public bool Contains(Point point)
        {
            if (Transform.HasInverse)
            {
                Point trPoint = point * Transform.Invert();

                return Bounds.Contains(trPoint);
            }
            else
            {
                return Bounds.Contains(point);
            }
        }
    }
}