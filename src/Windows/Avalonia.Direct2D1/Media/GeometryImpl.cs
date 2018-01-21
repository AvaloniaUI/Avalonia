// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// The platform-specific interface for <see cref="Avalonia.Media.Geometry"/>.
    /// </summary>
    public abstract class GeometryImpl : IGeometryImpl
    {
        public GeometryImpl(Geometry geometry)
        {
            Geometry = geometry;
        }

        /// <inheritdoc/>
        public Rect Bounds => Geometry.GetWidenedBounds(0).ToAvalonia();

        /// <inheritdoc/>
        public Geometry Geometry { get; }

        /// <inheritdoc/>
        public Rect GetRenderBounds(Avalonia.Media.Pen pen)
        {
            var factory = AvaloniaLocator.Current.GetService<Factory>();
            return Geometry.GetWidenedBounds((float)pen.Thickness).ToAvalonia();
        }

        /// <inheritdoc/>
        public bool FillContains(Point point)
        {
            return Geometry.FillContainsPoint(point.ToSharpDX());
        }

        /// <inheritdoc/>
        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            var result = new PathGeometry(Geometry.Factory);

            using (var sink = result.Open())
            {
                Geometry.Combine(((GeometryImpl)geometry).Geometry, CombineMode.Intersect, sink);
                return new StreamGeometryImpl(result);
            }
        }

        /// <inheritdoc/>
        public bool StrokeContains(Avalonia.Media.Pen pen, Point point)
        {
            return Geometry.StrokeContainsPoint(point.ToSharpDX(), (float)pen.Thickness);
        }

        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            var factory = AvaloniaLocator.Current.GetService<Factory>();
            return new TransformedGeometryImpl(
                new TransformedGeometry(
                    factory,
                    GetSourceGeometry(),
                    transform.ToDirect2D()),
                this);
        }

        protected virtual Geometry GetSourceGeometry() => Geometry;
    }
}
