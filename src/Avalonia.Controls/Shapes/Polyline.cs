using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Data;

namespace Avalonia.Controls.Shapes
{
    public class Polyline : Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            AvaloniaProperty.Register<Polyline, IList<Point>>("Points");

        public static readonly StyledProperty<FillRule> FillRuleProperty =
            AvaloniaProperty.Register<Polyline, FillRule>(nameof(FillRule));

        static Polyline()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Polyline>(1);
            AffectsGeometry<Polyline>(PointsProperty, FillRuleProperty);
        }

        public Polyline()
        {
            SetValue(PointsProperty, new Points(), BindingPriority.Template);
        }

        public IList<Point> Points
        {
            get => GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        /// <summary>
        /// Gets or sets how the interior of the polyline is determined when a <see cref="Shape.Fill"/> is applied.
        /// </summary>
        public FillRule FillRule
        {
            get => GetValue(FillRuleProperty);
            set => SetValue(FillRuleProperty, value);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            var isFilled = Fill != null;
            return new PolylineGeometry(Points, isFilled, FillRule);
        }
    }
}
