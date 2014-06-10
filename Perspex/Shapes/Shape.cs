// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Shapes
{
    using Perspex.Controls;
    using Perspex.Media;

    public class Shape : Control
    {
        public PerspexProperty<Brush> FillProperty =
            PerspexProperty.Register<Shape, Brush>("Fill");

        public PerspexProperty<Brush> StrokeProperty =
            PerspexProperty.Register<Shape, Brush>("Stroke");

        public PerspexProperty<double> StrokeThicknessProperty =
            PerspexProperty.Register<Shape, double>("StrokeThickness");

        public Brush Fill
        {
            get { return this.GetValue(FillProperty); }
            set { this.SetValue(FillProperty, value); }
        }

        public Brush Stroke
        {
            get { return this.GetValue(StrokeProperty); }
            set { this.SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return this.GetValue(StrokeThicknessProperty); }
            set { this.SetValue(StrokeThicknessProperty, value); }
        }
    }
}
