// -----------------------------------------------------------------------
// <copyright file="IStreamGeometryContextImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using Perspex.Media;

    public interface IStreamGeometryContextImpl : IDisposable
    {
        void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection);

        void BeginFigure(Point startPoint, bool isFilled);

        void BezierTo(Point point1, Point point2, Point point3);

        void LineTo(Point point);

        void EndFigure(bool isClosed);
    }
}
