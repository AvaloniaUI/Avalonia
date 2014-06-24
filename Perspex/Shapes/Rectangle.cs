// -----------------------------------------------------------------------
// <copyright file="Rectangle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Shapes
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Media;

    public class Rectangle : Shape
    {
        private Size size;

        public override Geometry DefiningGeometry
        {
            get { return new RectangleGeometry(new Rect(0, 0, this.Width, this.Height)); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return this.DefiningGeometry.GetRenderBounds(this.StrokeThickness).Size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            this.size = finalSize;
            return base.ArrangeOverride(finalSize);
        }
    }
}
