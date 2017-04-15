// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;
using System.Collections.Generic;

namespace Avalonia.Cairo.Media
{
    using Cairo = global::Cairo;

    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        public StreamGeometryImpl()
        {
            _impl = new StreamGeometryContextImpl(this, null);
        }

        public StreamGeometryImpl(StreamGeometryContextImpl impl)
        {
            _impl = impl;
        }

        public Rect Bounds
        {
			get { return _impl.Bounds; }
		} 

		public Cairo.Path Path 
		{
			get { return _impl.Path; }
		}

        private readonly StreamGeometryContextImpl _impl;

        private Matrix _transform = Matrix.Identity;

        public Matrix Transform
        {
            get { return _transform; }
            private set { _transform = value; }
        }

        public FillRule FillRule { get; set; }

        public IStreamGeometryImpl Clone()
		{
			return new StreamGeometryImpl(_impl);
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
            return Bounds.TransformToAABB(Transform).Inflate(strokeThickness);
        }

        public IStreamGeometryContextImpl Open()
        {
            return _impl;
        }

        public bool FillContains(Point point)
        {
            return _impl.FillContains(point);
        }

        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            throw new NotImplementedException();
        }

        public bool StrokeContains(Pen pen, Point point)
        {
            return _impl.StrokeContains(pen, point);
        }

        /// <inheritdoc/>
        public IGeometryImpl WithTransform(Matrix transform)
        {
            var result = (StreamGeometryImpl)Clone();
            result.Transform = transform;
            return result;
        }
    }
}
