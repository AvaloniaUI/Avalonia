// -----------------------------------------------------------------------
// <copyright file="GeometryImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using Perspex.Platform;
    using SharpDX.Direct2D1;
    using Splat;

    public abstract class GeometryImpl : IGeometryImpl
    {
        private TransformedGeometry transformed;

        public abstract Rect Bounds
        {
            get;
        }

        public abstract Geometry DefiningGeometry
        {
            get;
        }

        public Geometry Geometry
        {
            get { return this.transformed ?? this.DefiningGeometry; }
        }

        public Matrix Transform
        {
            get
            {
                return this.transformed != null ?
                    this.transformed.Transform.ToPerspex() :
                    Matrix.Identity;
            }

            set
            {
                if (value != this.Transform)
                {
                    if (this.transformed != null)
                    {
                        this.transformed.Dispose();
                        this.transformed = null;
                    }

                    if (!value.IsIdentity)
                    {
                        Factory factory = Locator.Current.GetService<Factory>();
                        this.transformed = new TransformedGeometry(
                            factory, 
                            this.DefiningGeometry, 
                            value.ToDirect2D());
                    }
                }
            }
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            if (this.transformed != null)
            {
                return this.transformed.GetWidenedBounds((float)strokeThickness).ToPerspex();
            }
            else
            {
                return this.DefiningGeometry.GetWidenedBounds((float)strokeThickness).ToPerspex();
            }
        }
    }
}
