// -----------------------------------------------------------------------
// <copyright file="StreamGeometryImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
    using Cairo = global::Cairo;
    using Splat;
    using System.Collections.Generic;

    public enum CairoGeometryType
    {
        Begin,
        ArcTo,
        LineTo,
        End
    }

    public class BeginOp : GeometryOp
    {
        public Point Point { get; set; }
        public bool IsFilled { get; set; }
    }

    public class EndOp : GeometryOp
    {
        public bool IsClosed { get; set; }
    }

    public class LineToOp : GeometryOp
    {
        public Point Point { get; set; }
    }

    public class CurveToOp : GeometryOp
    {
        public Point Point { get; set; }
        public Point Point2 { get; set; }
        public Point Point3 { get; set; }
    }

    public abstract class GeometryOp
    {
    }

    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        public StreamGeometryImpl()
        {
            this.Operations = new Queue<GeometryOp>();
        }

        public StreamGeometryImpl(Queue<GeometryOp> ops)
        {
            this.Operations = ops;
        }

        public Queue<GeometryOp> Operations
        {
            get;
            private set;
        }

        public Rect Bounds
        {
            get;
            set;
        }

        // TODO: Implement
        private Matrix transform = Matrix.Identity;
        public Matrix Transform
        {
            get { return transform; }
            set
            {
                if (value != this.Transform)
                {
                     if (!value.IsIdentity)
                     {
                        this.transform = value;
                     }
                }
            }
        }

        public IStreamGeometryImpl Clone()
        {
            // TODO: Implement
            return new StreamGeometryImpl(this.Operations);
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Implement
            return this.Bounds;
        }

        public IStreamGeometryContextImpl Open()
        {
            // TODO: Implement
            return new StreamGeometryContextImpl(this.Operations, this);
        }
    }
}
