// -----------------------------------------------------------------------
// <copyright file="Geometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Perspex.Platform;

    public abstract class Geometry : PerspexObject
    {
        public static readonly PerspexProperty<ITransform> TransformProperty =
            PerspexProperty.Register<Geometry, ITransform>("Transform");

        static Geometry()
        {
            TransformProperty.Changed.Subscribe(x =>
            {
                ((Geometry)x.Sender).PlatformImpl.Transform = ((ITransform)x.NewValue).Value;
            });
        }

        public abstract Rect Bounds
        {
            get;
        }

        public IGeometryImpl PlatformImpl
        {
            get;
            protected set;
        }

        public ITransform Transform
        {
            get { return this.GetValue(TransformProperty); }
            set { this.SetValue(TransformProperty, value); }
        }

        public abstract Geometry Clone();

        public Rect GetRenderBounds(double strokeThickness)
        {
            return this.PlatformImpl.GetRenderBounds(strokeThickness);
        }
    }
}
