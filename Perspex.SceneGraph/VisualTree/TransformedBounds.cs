// -----------------------------------------------------------------------
// <copyright file="TransformedBounds.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.VisualTree
{
    public class TransformedBounds
    {
        public TransformedBounds(Rect bounds, Rect clip, Matrix transform)
        {
            this.Bounds = bounds;
            this.Clip = clip;
            this.Transform = transform;
        }

        public Rect Bounds { get; }

        public Rect Clip { get; }

        public Matrix Transform { get;  }
    }
}
