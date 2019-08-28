// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        public Geometry Geometry { get; }

        /// <inheritdoc/>
        public Rect GetRenderBounds(Avalonia.Media.IPen pen)
        {
            return Geometry.GetWidenedBounds((float)(pen?.Thickness ?? 0)).ToAvalonia();
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
        public bool StrokeContains(Avalonia.Media.IPen pen, Point point)
        {
            return Geometry.StrokeContainsPoint(point.ToSharpDX(), (float)(pen?.Thickness ?? 0));
        }

        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(
                new TransformedGeometry(
                    Direct2D1Platform.Direct2D1Factory,
                    GetSourceGeometry(),
                    transform.ToDirect2D()),
                this);
        }

        protected virtual Geometry GetSourceGeometry() => Geometry;
    }
}
