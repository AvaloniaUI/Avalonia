using System;

namespace Avalonia.Media
{
    public sealed class ArcSegment : PathSegment
    {
        /// <summary>
        /// Defines the <see cref="IsLargeArc"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsLargeArcProperty
                     = AvaloniaProperty.Register<ArcSegment, bool>(nameof(IsLargeArc), false);

        /// <summary>
        /// Defines the <see cref="Point"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> PointProperty
                                = AvaloniaProperty.Register<ArcSegment, Point>(nameof(Point));

        /// <summary>
        /// Defines the <see cref="RotationAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RotationAngleProperty
                                     = AvaloniaProperty.Register<ArcSegment, double>(nameof(RotationAngle), 0);

        /// <summary>
        /// Defines the <see cref="Size"/> property.
        /// </summary>
        public static readonly StyledProperty<Size> SizeProperty
                    = AvaloniaProperty.Register<ArcSegment, Size>(nameof(Size));

        /// <summary>
        /// Defines the <see cref="SweepDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<SweepDirection> SweepDirectionProperty
                = AvaloniaProperty.Register<ArcSegment, SweepDirection>(nameof(SweepDirection), SweepDirection.Clockwise);

        /// <summary>
        /// Gets or sets a value indicating whether the arc should follow the longer path around an ellipse rather than the shorter one.
        /// </summary>
        /// <remarks>
        /// Set to <c>true</c> to draw the arc greater than 180 degrees; otherwise, <c>false</c>.
        /// </remarks>
        /// <value>
        /// <c>true</c> if this instance is a large arc; otherwise, <c>false</c>.
        /// </value>
        public bool IsLargeArc
        {
            get => GetValue(IsLargeArcProperty);
            set => SetValue(IsLargeArcProperty, value);
        }

        /// <summary>
        /// Gets or sets the destination point where the arc ends.
        /// </summary>
        /// <value>
        /// The destination point.
        /// </value>
        public Point Point
        {
            get => GetValue(PointProperty);
            set => SetValue(PointProperty, value);
        }

        /// <summary>
        /// Gets or sets the rotation angle (in degrees) of the ellipse that specifies the path of the arc; positive values are clockwise.
        /// </summary>
        /// <remarks>
        /// This will rotate the entire arc relative to the X-axis and is not commonly used.
        /// </remarks>
        /// <value>
        /// The rotation angle.
        /// </value>
        public double RotationAngle
        {
            get => GetValue(RotationAngleProperty);
            set => SetValue(RotationAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the radii of an ellipse whose path is used to draw the arc.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public Size Size
        {
            get => GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the sweep direction which indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </summary>
        /// <value>
        /// The sweep direction.
        /// </value>
        public SweepDirection SweepDirection
        {
            get => GetValue(SweepDirectionProperty);
            set => SetValue(SweepDirectionProperty, value);
        }

        internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.ArcTo(Point, Size, RotationAngle, IsLargeArc, SweepDirection, IsStroked);
        }

        public override string ToString()
            => FormattableString.Invariant($"A {Size} {RotationAngle} {(IsLargeArc ? 1 : 0)} {(int)SweepDirection} {Point}");
    }
}
