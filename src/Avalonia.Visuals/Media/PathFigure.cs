// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;

namespace Avalonia.Media
{
    public sealed class PathFigure : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="IsClosed"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsClosedProperty
                            = AvaloniaProperty.Register<PathFigure, bool>(nameof(IsClosed), true);
        /// <summary>
        /// Defines the <see cref="IsFilled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsFilledProperty
                            = AvaloniaProperty.Register<PathFigure, bool>(nameof(IsFilled), true);
        /// <summary>
        /// Defines the <see cref="Segments"/> property.
        /// </summary>
        public static readonly DirectProperty<PathFigure, PathSegments> SegmentsProperty
                        = AvaloniaProperty.RegisterDirect<PathFigure, PathSegments>(nameof(Segments), f => f.Segments, (f, s) => f.Segments = s);
        /// <summary>
        /// Defines the <see cref="StartPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> StartPointProperty
                        = AvaloniaProperty.Register<PathFigure, Point>(nameof(StartPoint));

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFigure"/> class.
        /// </summary>
        public PathFigure()
        {
            Segments = new PathSegments();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is closed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
        /// </value>
        public bool IsClosed
        {
            get { return GetValue(IsClosedProperty); }
            set { SetValue(IsClosedProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is filled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is filled; otherwise, <c>false</c>.
        /// </value>
        public bool IsFilled
        {
            get { return GetValue(IsFilledProperty); }
            set { SetValue(IsFilledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the segments.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        [Content]
        public PathSegments Segments
        {
            get { return _segments; }
            set { SetAndRaise(SegmentsProperty, ref _segments, value); }
        }

        /// <summary>
        /// Gets or sets the start point.
        /// </summary>
        /// <value>
        /// The start point.
        /// </value>
        public Point StartPoint
        {
            get { return GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        internal void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.BeginFigure(StartPoint, IsFilled);

            foreach (var segment in Segments)
            {
                segment.ApplyTo(ctx);
            }

            ctx.EndFigure(IsClosed);
        }

        private PathSegments _segments;
    }
}