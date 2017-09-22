// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of a line.
    /// </summary>
    public class LineGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="StartPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> StartPointProperty =
            AvaloniaProperty.Register<LineGeometry, Point>(nameof(StartPoint));

        public Point StartPoint
        {
            get => GetValue(StartPointProperty);
            set => SetValue(StartPointProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="EndPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> EndPointProperty =
            AvaloniaProperty.Register<LineGeometry, Point>(nameof(EndPoint));
        private bool _isDirty = true;

        public Point EndPoint
        {
            get => GetValue(EndPointProperty);
            set => SetValue(EndPointProperty, value);
        }

        static LineGeometry()
        {
            StartPointProperty.Changed.AddClassHandler<LineGeometry>(x => x.PointsChanged);
            EndPointProperty.Changed.AddClassHandler<LineGeometry>(x => x.PointsChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        public LineGeometry()
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = factory.CreateStreamGeometry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        public LineGeometry(Point startPoint, Point endPoint) : this()
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public override IGeometryImpl PlatformImpl
        {
            get
            {
                PrepareIfNeeded();
                return base.PlatformImpl;
            }
            protected set => base.PlatformImpl = value;
        }

        public void PrepareIfNeeded()
        {
            if (_isDirty)
            {
                _isDirty = false;

                using (var context = ((IStreamGeometryImpl)PlatformImpl).Open())
                {
                    context.BeginFigure(StartPoint, false);
                    context.LineTo(EndPoint);
                    context.EndFigure(false);
                }
            }
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            PrepareIfNeeded();
            return new LineGeometry(StartPoint, EndPoint);
        }

        private void PointsChanged(AvaloniaPropertyChangedEventArgs e) => _isDirty = true;
    }
}
