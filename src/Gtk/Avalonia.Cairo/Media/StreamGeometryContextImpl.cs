// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.RenderHelpers;

namespace Avalonia.Cairo.Media
{
    using Cairo = global::Cairo;

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private readonly StreamGeometryImpl _target;
        private Point _currentPoint;
		public StreamGeometryContextImpl(StreamGeometryImpl target, Cairo.Path path)
        {
		    _target = target;

		    _surf = new Cairo.ImageSurface (Cairo.Format.Argb32, 0, 0);
			_context = new Cairo.Context (_surf);
			this.Path = path;

			if (this.Path != null) 
			{
				_context.AppendPath(this.Path);
			}
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            ArcToHelper.ArcTo(this, _currentPoint, point, size, rotationAngle, isLargeArc, sweepDirection);
            _currentPoint = point;
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            if (this.Path == null)
            {
                _context.MoveTo(startPoint.ToCairo());
                _currentPoint = startPoint;
            }
        }

        public void CubicBezierTo(Point point1, Point point2, Point point3)
        {
            if (this.Path == null)
            {
                _context.CurveTo(point1.ToCairo(), point2.ToCairo(), point3.ToCairo());
                _currentPoint = point3;
            }
        }

        public void QuadraticBezierTo(Point control, Point endPoint)
        {
            if (this.Path == null)
            {
                QuadBezierHelper.QuadraticBezierTo(this, _currentPoint, control, endPoint);
                _currentPoint = endPoint;
            }
        }

        internal bool FillContains(Point point)
        {
            using (var context = new Cairo.Context(new Cairo.ImageSurface(Cairo.Format.Argb32, 0, 0)))
            {
                context.AppendPath(Path);
                return context.InFill(point.X, point.Y); 
            }
        }

        internal bool StrokeContains(Pen pen, Point point)
        {
            using (var context = new Cairo.Context(new Cairo.ImageSurface(Cairo.Format.Argb32, 0, 0)))
            {
                context.AppendPath(Path);
                return context.InStroke(point.X, point.Y);
            }
        }

        public void LineTo(Point point)
        {
            if (this.Path == null)
            {
                _context.LineTo(point.ToCairo());
                _currentPoint = point;
            }
        }

        private readonly Cairo.Context _context;
        private readonly Cairo.ImageSurface _surf;
		public Cairo.Path Path { get; private set; }
		public Rect Bounds { get; private set; }

        public void EndFigure(bool isClosed)
        {
			if (this.Path == null) 
			{
				if (isClosed)
					_context.ClosePath ();
			}
        }

        public void SetFillRule(FillRule fillRule)
        {
            _target.FillRule = fillRule;
        }


        public void Dispose()
        {
            if (this.Path == null)
            {
                Path = _context.CopyPath();
                Bounds = _context.FillExtents().ToAvalonia();
            }

            _context.Dispose ();
			_surf.Dispose ();
        }
    }
}
