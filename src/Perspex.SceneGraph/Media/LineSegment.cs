// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Media
{
    public sealed class LineSegment : PathSegment
    {
        /// <summary>
        /// Defines the <see cref="Point"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> PointProperty
                        = PerspexProperty.Register<LineSegment, Point>(nameof(Point));

        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>
        /// The point.
        /// </value>
        public Point Point
        {
            get { return GetValue(PointProperty); }
            set { SetValue(PointProperty, value); }
        }

        protected internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.LineTo(Point);
        }
    }
}