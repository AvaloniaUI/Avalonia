// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Media;
using Perspex.Platform;
using System.Collections.Generic;

namespace Perspex.Cairo.Media
{
    using Cairo = global::Cairo;

    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        public StreamGeometryImpl()
        {
            _impl = new StreamGeometryContextImpl(null);
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
            set
            {
                if (value != Transform)
                {
                    if (!value.IsIdentity)
                    {
                        _transform = value;
                    }
                }
            }
        }

        public IStreamGeometryImpl Clone()
		{
			return new StreamGeometryImpl(_impl);
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
			return Bounds.Inflate(strokeThickness);
        }

        public IStreamGeometryContextImpl Open()
        {
            return _impl;
        }
    }
}
