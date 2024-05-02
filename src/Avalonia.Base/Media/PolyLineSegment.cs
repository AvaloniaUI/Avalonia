using System.Collections.Generic;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a set of line segments defined by a points collection with each Point specifying the end point of a line segment.
    /// </summary>
    public sealed class PolyLineSegment : PathSegment
    {
        /// <summary>
        /// Defines the <see cref="Points"/> property.
        /// </summary>
        public static readonly StyledProperty<IList<Point>> PointsProperty
            = AvaloniaProperty.Register<PolyLineSegment, IList<Point>>(nameof(Points));

        /// <summary>
        /// Gets or sets the points.
        /// </summary>
        /// <value>
        /// The points.
        /// </value>
        public IList<Point> Points
        {
            get => GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolyLineSegment"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012", 
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public PolyLineSegment()
        {
            Points = new Points();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolyLineSegment"/> class.
        /// </summary>
        /// <param name="points">The points.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012",
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public PolyLineSegment(IEnumerable<Point> points)
        {
            Points = new Points(points);
        }

        internal override void ApplyTo(StreamGeometryContext ctx)
        {
            var points = Points;
            if (points.Count > 0)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    ctx.LineTo(points[i]);
                }
            }
        }

        public override string ToString()
            => Points.Count >= 1 ? "L " + string.Join(" ", Points) : "";
    }
}
