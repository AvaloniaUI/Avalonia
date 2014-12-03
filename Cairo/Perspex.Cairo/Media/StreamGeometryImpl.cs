// -----------------------------------------------------------------------
// <copyright file="Direct2DStreamGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;

    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        public StreamGeometryImpl()
        {
            // TODO: Implement
        }

        public Rect Bounds
        {
            get { return new Rect(); }
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Implement
            return new Rect();
        }

        public IStreamGeometryContextImpl Open()
        {
            // TODO: Implement
            return new StreamGeometryContextImpl();
        }
    }
}
