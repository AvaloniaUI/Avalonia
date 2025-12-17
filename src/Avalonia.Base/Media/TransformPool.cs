using System;
using System.Collections.Concurrent;

namespace Avalonia.Media
{
    /// <summary>
    /// Provides pooled transform operations to reduce allocations.
    /// </summary>
    /// <remarks>
    /// Creating new transform objects (e.g., <c>new RotateTransform(angle)</c>) allocates memory.
    /// For scenarios with frequent transform updates (animations, drag operations), 
    /// use these pooled transforms or update existing transform properties directly.
    /// </remarks>
    public static class TransformPool
    {
        private static readonly ConcurrentBag<RotateTransform> s_rotatePool = new();
        private static readonly ConcurrentBag<ScaleTransform> s_scalePool = new();
        private static readonly ConcurrentBag<TranslateTransform> s_translatePool = new();
        private static readonly ConcurrentBag<SkewTransform> s_skewPool = new();
        
        private const int MaxPoolSize = 64;

        /// <summary>
        /// Gets a pooled <see cref="RotateTransform"/> with the specified angle.
        /// </summary>
        /// <param name="angle">The rotation angle in degrees.</param>
        /// <returns>A pooled or new <see cref="RotateTransform"/>.</returns>
        /// <remarks>
        /// Call <see cref="Return(RotateTransform)"/> when done to return the transform to the pool.
        /// If you don't return it, it will simply be garbage collected normally.
        /// </remarks>
        public static RotateTransform GetRotate(double angle)
        {
            if (!s_rotatePool.TryTake(out var transform))
            {
                transform = new RotateTransform();
            }
            transform.Angle = angle;
            transform.CenterX = 0;
            transform.CenterY = 0;
            return transform;
        }

        /// <summary>
        /// Gets a pooled <see cref="RotateTransform"/> with the specified angle and center.
        /// </summary>
        /// <param name="angle">The rotation angle in degrees.</param>
        /// <param name="centerX">The X center of rotation.</param>
        /// <param name="centerY">The Y center of rotation.</param>
        /// <returns>A pooled or new <see cref="RotateTransform"/>.</returns>
        public static RotateTransform GetRotate(double angle, double centerX, double centerY)
        {
            if (!s_rotatePool.TryTake(out var transform))
            {
                transform = new RotateTransform();
            }
            transform.Angle = angle;
            transform.CenterX = centerX;
            transform.CenterY = centerY;
            return transform;
        }

        /// <summary>
        /// Returns a <see cref="RotateTransform"/> to the pool for reuse.
        /// </summary>
        /// <param name="transform">The transform to return.</param>
        public static void Return(RotateTransform? transform)
        {
            if (transform != null && s_rotatePool.Count < MaxPoolSize)
            {
                s_rotatePool.Add(transform);
            }
        }

        /// <summary>
        /// Gets a pooled <see cref="ScaleTransform"/> with the specified scale factors.
        /// </summary>
        /// <param name="scaleX">The X scale factor.</param>
        /// <param name="scaleY">The Y scale factor.</param>
        /// <returns>A pooled or new <see cref="ScaleTransform"/>.</returns>
        public static ScaleTransform GetScale(double scaleX, double scaleY)
        {
            if (!s_scalePool.TryTake(out var transform))
            {
                transform = new ScaleTransform();
            }
            transform.ScaleX = scaleX;
            transform.ScaleY = scaleY;
            return transform;
        }

        /// <summary>
        /// Gets a pooled <see cref="ScaleTransform"/> with uniform scale.
        /// </summary>
        /// <param name="scale">The uniform scale factor.</param>
        /// <returns>A pooled or new <see cref="ScaleTransform"/>.</returns>
        public static ScaleTransform GetScale(double scale) => GetScale(scale, scale);

        /// <summary>
        /// Returns a <see cref="ScaleTransform"/> to the pool for reuse.
        /// </summary>
        /// <param name="transform">The transform to return.</param>
        public static void Return(ScaleTransform? transform)
        {
            if (transform != null && s_scalePool.Count < MaxPoolSize)
            {
                s_scalePool.Add(transform);
            }
        }

        /// <summary>
        /// Gets a pooled <see cref="TranslateTransform"/> with the specified offset.
        /// </summary>
        /// <param name="x">The X translation offset.</param>
        /// <param name="y">The Y translation offset.</param>
        /// <returns>A pooled or new <see cref="TranslateTransform"/>.</returns>
        public static TranslateTransform GetTranslate(double x, double y)
        {
            if (!s_translatePool.TryTake(out var transform))
            {
                transform = new TranslateTransform();
            }
            transform.X = x;
            transform.Y = y;
            return transform;
        }

        /// <summary>
        /// Returns a <see cref="TranslateTransform"/> to the pool for reuse.
        /// </summary>
        /// <param name="transform">The transform to return.</param>
        public static void Return(TranslateTransform? transform)
        {
            if (transform != null && s_translatePool.Count < MaxPoolSize)
            {
                s_translatePool.Add(transform);
            }
        }

        /// <summary>
        /// Gets a pooled <see cref="SkewTransform"/> with the specified angles.
        /// </summary>
        /// <param name="angleX">The X skew angle in degrees.</param>
        /// <param name="angleY">The Y skew angle in degrees.</param>
        /// <returns>A pooled or new <see cref="SkewTransform"/>.</returns>
        public static SkewTransform GetSkew(double angleX, double angleY)
        {
            if (!s_skewPool.TryTake(out var transform))
            {
                transform = new SkewTransform();
            }
            transform.AngleX = angleX;
            transform.AngleY = angleY;
            return transform;
        }

        /// <summary>
        /// Returns a <see cref="SkewTransform"/> to the pool for reuse.
        /// </summary>
        /// <param name="transform">The transform to return.</param>
        public static void Return(SkewTransform? transform)
        {
            if (transform != null && s_skewPool.Count < MaxPoolSize)
            {
                s_skewPool.Add(transform);
            }
        }

        /// <summary>
        /// Updates a <see cref="RotateTransform"/> in place, or creates/gets a pooled one if the current transform is not a <see cref="RotateTransform"/>.
        /// </summary>
        /// <param name="current">The current transform (may be null or different type).</param>
        /// <param name="angle">The new rotation angle.</param>
        /// <returns>The updated or new transform.</returns>
        /// <remarks>
        /// This is the recommended pattern for efficient transform updates:
        /// <code>
        /// control.RenderTransform = TransformOperations.SetRotation(control.RenderTransform, angle);
        /// </code>
        /// </remarks>
        public static RotateTransform SetRotation(Transform? current, double angle)
        {
            if (current is RotateTransform rotate)
            {
                rotate.Angle = angle;
                return rotate;
            }
            return GetRotate(angle);
        }

        /// <summary>
        /// Updates a <see cref="ScaleTransform"/> in place, or creates/gets a pooled one if the current transform is not a <see cref="ScaleTransform"/>.
        /// </summary>
        /// <param name="current">The current transform (may be null or different type).</param>
        /// <param name="scaleX">The new X scale factor.</param>
        /// <param name="scaleY">The new Y scale factor.</param>
        /// <returns>The updated or new transform.</returns>
        public static ScaleTransform SetScale(Transform? current, double scaleX, double scaleY)
        {
            if (current is ScaleTransform scale)
            {
                scale.ScaleX = scaleX;
                scale.ScaleY = scaleY;
                return scale;
            }
            return GetScale(scaleX, scaleY);
        }

        /// <summary>
        /// Updates a <see cref="TranslateTransform"/> in place, or creates/gets a pooled one if the current transform is not a <see cref="TranslateTransform"/>.
        /// </summary>
        /// <param name="current">The current transform (may be null or different type).</param>
        /// <param name="x">The new X offset.</param>
        /// <param name="y">The new Y offset.</param>
        /// <returns>The updated or new transform.</returns>
        public static TranslateTransform SetTranslation(Transform? current, double x, double y)
        {
            if (current is TranslateTransform translate)
            {
                translate.X = x;
                translate.Y = y;
                return translate;
            }
            return GetTranslate(x, y);
        }
    }
}
