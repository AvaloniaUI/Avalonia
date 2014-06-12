// -----------------------------------------------------------------------
// <copyright file="GeometryImpl.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Media;
    using SharpDX.Direct2D1;
    using Splat;

    public abstract class GeometryImpl : IGeometryImpl
    {
        public abstract Rect Bounds
        {
            get;
        }

        public SharpDX.Direct2D1.Geometry Geometry
        {
            get;
            protected set;
        }
    }
}
