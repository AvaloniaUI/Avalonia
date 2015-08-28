﻿// -----------------------------------------------------------------------
// <copyright file="TransformedBounds.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.VisualTree
{
    /// <summary>
    /// Holds information about the bounds of a control, together with a transform and a clip/
    /// </summary>
    public class TransformedBounds
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformedBounds"/> class.
        /// </summary>
        /// <param name="bounds">The control's bounds.</param>
        /// <param name="clip">The control's clip rectangle.</param>
        /// <param name="transform">The control's transform.</param>
        public TransformedBounds(Rect bounds, Rect clip, Matrix transform)
        {
            this.Bounds = bounds;
            this.Clip = clip;
            this.Transform = transform;
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
        public Matrix Transform { get;  }
    }
}
