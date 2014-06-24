// -----------------------------------------------------------------------
// <copyright file="Rectangle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Shapes
{
    using Perspex.Media;

    public class Rectangle : Shape
    {
        public override Geometry DefiningGeometry
        {
            get { return new RectangleGeometry(new Rect(0, 0, this.Width, this.Height)); }
        }
    }
}
