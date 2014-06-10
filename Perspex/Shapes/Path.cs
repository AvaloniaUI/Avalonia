// -----------------------------------------------------------------------
// <copyright file="Path.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Shapes
{
    using Perspex.Media;

    public class Path : Shape
    {
        public PerspexProperty<Geometry> DataProperty =
            PerspexProperty.Register<Path, Geometry>("Data");

        public Geometry Data
        {
            get { return this.GetValue(DataProperty); }
            set { this.SetValue(DataProperty, value); }
        }

        public override void Render(IDrawingContext context)
        {
            context.DrawGeometry(this.Fill, new Pen(this.Stroke, this.StrokeThickness), this.Data);
        }
    }
}
