using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that have bounds.
    /// </summary>
    internal abstract class DrawOperation : IDrawOperation
    {
        public DrawOperation(Rect bounds, Matrix transform)
        {
            bounds = bounds.Normalize().TransformToAABB(transform);

            Bounds = new Rect(
                new Point(Math.Floor(bounds.X), Math.Floor(bounds.Y)),
                new Point(Math.Ceiling(bounds.Right), Math.Ceiling(bounds.Bottom)));
        }

        public Rect Bounds { get; }

        public abstract bool HitTest(Point p);

        public abstract void Render(IDrawingContextImpl context);

        public virtual void Dispose()
        {
        }
    }

    internal abstract class DrawOperationWithTransform : DrawOperation, IDrawOperationWithTransform
    {
        protected DrawOperationWithTransform(Rect bounds, Matrix transform) : base(bounds, transform)
        {
            Transform = transform;
        }

        public Matrix Transform { get; }

        public sealed override bool HitTest(Point p)
        {
            if (Transform.IsIdentity)
                return HitTestTransformed(p);

            if (!Transform.HasInverse)
                return false;

            var transformedPoint = Transform.Invert().Transform(p);

            return HitTestTransformed(transformedPoint);
        }

        public abstract bool HitTestTransformed(Point p);
    }
}
