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

    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        public StreamGeometryImpl()
        {
            this.impl = new StreamGeometryContextImpl(this);
        }

        public StreamGeometryImpl(Cairo.Path path)
        {
            this.impl = new StreamGeometryContextImpl(this);
            this.Path = path;
        }
        
        public Cairo.Path Path
        {
            get;
            set;
        }

        public Rect Bounds
        {
            get;
            set;
        }

        private StreamGeometryContextImpl impl;

        private Matrix transform = Matrix.Identity;
        public Matrix Transform
        {
            get { return this.transform; }
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
            return new StreamGeometryImpl(this.Path);
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            return this.Bounds;
        }

        public IStreamGeometryContextImpl Open()
        {
            return this.impl;
        }
    }
}
