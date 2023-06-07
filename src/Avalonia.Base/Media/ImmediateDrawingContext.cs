using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    public sealed class ImmediateDrawingContext : IDisposable, IOptionalFeatureProvider
    {
        private readonly bool _ownsImpl;
        private int _currentLevel;
        
        private static ThreadSafeObjectPool<Stack<PushedState>> StateStackPool { get; } =
            ThreadSafeObjectPool<Stack<PushedState>>.Default;

        private static ThreadSafeObjectPool<Stack<TransformContainer>> TransformStackPool { get; } =
            ThreadSafeObjectPool<Stack<TransformContainer>>.Default;

        private Stack<PushedState>? _states = StateStackPool.Get();

        private Stack<TransformContainer>? _transformContainers = TransformStackPool.Get();

        readonly struct TransformContainer
        {
            public readonly Matrix LocalTransform;
            public readonly Matrix ContainerTransform;

            public TransformContainer(Matrix localTransform, Matrix containerTransform)
            {
                LocalTransform = localTransform;
                ContainerTransform = containerTransform;
            }
        }

        internal ImmediateDrawingContext(IDrawingContextImpl impl, bool ownsImpl)
        {
            _ownsImpl = ownsImpl;
            PlatformImpl = impl;
            _currentContainerTransform = impl.Transform;
        }

        public IDrawingContextImpl PlatformImpl { get; }

        private Matrix _currentTransform = Matrix.Identity;

        private Matrix _currentContainerTransform;

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get { return _currentTransform; }
            private set
            {
                _currentTransform = value;
                var transform = _currentTransform * _currentContainerTransform;
                PlatformImpl.Transform = transform;
            }
        }

        /// <summary>
        /// Draws an bitmap.
        /// </summary>
        /// <param name="source">The bitmap.</param>
        /// <param name="rect">The rect in the output to draw to.</param>
        public void DrawBitmap(Bitmap source, Rect rect)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            DrawBitmap(source, new Rect(source.Size), rect);
        }

        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="source">The bitmap.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        public void DrawBitmap(Bitmap source, Rect sourceRect, Rect destRect)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            PlatformImpl.DrawBitmap(source.PlatformImpl.Item, 1, sourceRect, destRect);
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p2">The second point of the line.</param>
        public void DrawLine(ImmutablePen pen, Point p1, Point p2)
        {
            if (PenIsVisible(pen))
            {
                PlatformImpl.DrawLine(pen, p1, p2);
            }
        }

        /// <summary>
        /// Draws a rectangle with the specified Brush and Pen.
        /// </summary>
        /// <param name="brush">The brush used to fill the rectangle, or <c>null</c> for no fill.</param>
        /// <param name="pen">The pen used to stroke the rectangle, or <c>null</c> for no stroke.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="radiusX">The radius in the X dimension of the rounded corners.
        ///     This value will be clamped to the range of 0 to Width/2
        /// </param>
        /// <param name="radiusY">The radius in the Y dimension of the rounded corners.
        ///     This value will be clamped to the range of 0 to Height/2
        /// </param>
        /// <param name="boxShadows">Box shadow effect parameters</param>
        /// <remarks>
        /// The brush and the pen can both be null. If the brush is null, then no fill is performed.
        /// If the pen is null, then no stoke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        public void DrawRectangle(IImmutableBrush? brush, ImmutablePen? pen, Rect rect, double radiusX = 0, double radiusY = 0,
            BoxShadows boxShadows = default)
        {
            if (brush == null && !PenIsVisible(pen))
            {
                return;
            }

            if (!MathUtilities.IsZero(radiusX))
            {
                radiusX = Math.Min(radiusX, rect.Width / 2);
            }

            if (!MathUtilities.IsZero(radiusY))
            {
                radiusY = Math.Min(radiusY, rect.Height / 2);
            }

            PlatformImpl.DrawRectangle(brush, pen, new RoundedRect(rect, radiusX, radiusY), boxShadows);
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void DrawRectangle(ImmutablePen pen, Rect rect, float cornerRadius = 0.0f)
        {
            DrawRectangle(null, pen, rect, cornerRadius, cornerRadius);
        }

        /// <summary>
        /// Draws an ellipse with the specified Brush and Pen.
        /// </summary>
        /// <param name="brush">The brush used to fill the ellipse, or <c>null</c> for no fill.</param>
        /// <param name="pen">The pen used to stroke the ellipse, or <c>null</c> for no stroke.</param>
        /// <param name="center">The location of the center of the ellipse.</param>
        /// <param name="radiusX">The horizontal radius of the ellipse.</param>
        /// <param name="radiusY">The vertical radius of the ellipse.</param>
        /// <remarks>
        /// The brush and the pen can both be null. If the brush is null, then no fill is performed.
        /// If the pen is null, then no stoke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        public void DrawEllipse(IImmutableBrush? brush, ImmutablePen? pen, Point center, double radiusX, double radiusY)
        {
            if (brush == null && !PenIsVisible(pen))
            {
                return;
            }

            var originX = center.X - radiusX;
            var originY = center.Y - radiusY;
            var width = radiusX * 2;
            var height = radiusY * 2;

            PlatformImpl.DrawEllipse(brush, pen, new Rect(originX, originY, width, height));
        }

        /// <summary>
        /// Draws a glyph run.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="glyphRun">The glyph run.</param>
        public void DrawGlyphRun(IImmutableBrush foreground, IImmutableGlyphRunReference glyphRun)
        {
            _ = glyphRun ?? throw new ArgumentNullException(nameof(glyphRun));
            if (glyphRun.GlyphRun != null)
                PlatformImpl.DrawGlyphRun(foreground, glyphRun.GlyphRun.Item);
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectangle(IImmutableBrush brush, Rect rect, float cornerRadius = 0.0f)
        {
            DrawRectangle(brush, null, rect, cornerRadius, cornerRadius);
        }

        public readonly record struct PushedState : IDisposable
        {
            private readonly int _level;
            private readonly ImmediateDrawingContext _context;
            private readonly Matrix _matrix;
            private readonly PushedStateType _type;

            public enum PushedStateType
            {
                None,
                Matrix,
                Opacity,
                Clip,
                MatrixContainer,
                GeometryClip,
                OpacityMask,
            }

            internal PushedState(ImmediateDrawingContext context, PushedStateType type, Matrix matrix = default)
            {
                if (context._states is null)
                    throw new ObjectDisposedException(nameof(ImmediateDrawingContext));

                _context = context;
                _type = type;
                _matrix = matrix;
                _level = context._currentLevel += 1;
                context._states.Push(this);
            }

            public void Dispose()
            {
                if (_type == PushedStateType.None)
                    return;
                if (_context._states is null || _context._transformContainers is null)
                    throw new ObjectDisposedException(nameof(DrawingContext));
                if (_context._currentLevel != _level)
                    throw new InvalidOperationException("Wrong Push/Pop state order");
                _context._currentLevel--;
                _context._states.Pop();
                if (_type == PushedStateType.Matrix)
                    _context.CurrentTransform = _matrix;
                else if (_type == PushedStateType.Clip)
                    _context.PlatformImpl.PopClip();
                else if (_type == PushedStateType.Opacity)
                    _context.PlatformImpl.PopOpacity();
                else if (_type == PushedStateType.GeometryClip)
                    _context.PlatformImpl.PopGeometryClip();
                else if (_type == PushedStateType.OpacityMask)
                    _context.PlatformImpl.PopOpacityMask();
                else if (_type == PushedStateType.MatrixContainer)
                {
                    var cont = _context._transformContainers.Pop();
                    _context._currentContainerTransform = cont.ContainerTransform;
                    _context.CurrentTransform = cont.LocalTransform;
                }
            }
        }


        public PushedState PushClip(RoundedRect clip)
        {
            PlatformImpl.PushClip(clip);
            return new PushedState(this, PushedState.PushedStateType.Clip);
        }

        /// <summary>
        /// Pushes a clip rectangle.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public PushedState PushClip(Rect clip)
        {
            PlatformImpl.PushClip(clip);
            return new PushedState(this, PushedState.PushedStateType.Clip);
        }

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public PushedState PushOpacity(double opacity, Rect bounds)
        //TODO: Eliminate platform-specific push opacity call
        {
            PlatformImpl.PushOpacity(opacity, bounds);
            return new PushedState(this, PushedState.PushedStateType.Opacity);
        }

        /// <summary>
        /// Pushes an opacity mask.
        /// </summary>
        /// <param name="mask">The opacity mask.</param>
        /// <param name="bounds">
        /// The size of the brush's target area. TODO: Are we sure this is needed?
        /// </param>
        /// <returns>A disposable to undo the opacity mask.</returns>
        public PushedState PushOpacityMask(IImmutableBrush mask, Rect bounds)
        {
            PlatformImpl.PushOpacityMask(mask, bounds);
            return new PushedState(this, PushedState.PushedStateType.OpacityMask);
        }

        /// <summary>
        /// Pushes a matrix post-transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPostTransform(Matrix matrix) => PushSetTransform(CurrentTransform * matrix);

        /// <summary>
        /// Pushes a matrix pre-transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPreTransform(Matrix matrix) => PushSetTransform(matrix * CurrentTransform);

        /// <summary>
        /// Sets the current matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushSetTransform(Matrix matrix)
        {
            var oldMatrix = CurrentTransform;
            CurrentTransform = matrix;

            return new PushedState(this, PushedState.PushedStateType.Matrix, oldMatrix);
        }

        /// <summary>
        /// Pushes a new transform context.
        /// </summary>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushTransformContainer()
        {
            if (_transformContainers is null)
                throw new ObjectDisposedException(nameof(DrawingContext));
            _transformContainers.Push(new TransformContainer(CurrentTransform, _currentContainerTransform));
            _currentContainerTransform = CurrentTransform * _currentContainerTransform;
            _currentTransform = Matrix.Identity;
            return new PushedState(this, PushedState.PushedStateType.MatrixContainer);
        }

        /// <summary>
        /// Disposes of any resources held by the <see cref="DrawingContext"/>.
        /// </summary>
        public void Dispose()
        {
            if (_states is null || _transformContainers is null)
                throw new ObjectDisposedException(nameof(DrawingContext));
            while (_states.Count != 0)
                _states.Peek().Dispose();
            StateStackPool.ReturnAndSetNull(ref _states);
            if (_transformContainers.Count != 0)
                throw new InvalidOperationException("Transform container stack is non-empty");
            TransformStackPool.ReturnAndSetNull(ref _transformContainers);
            if (_ownsImpl)
                PlatformImpl.Dispose();
        }

        private static bool PenIsVisible(IPen? pen)
        {
            return pen?.Brush != null && pen.Thickness > 0;
        }

        public object? TryGetFeature(Type type) => PlatformImpl.GetFeature(type);
    }
}
