using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public abstract class DrawingContext : IDisposable
    {
        private static ThreadSafeObjectPool<Stack<RestoreState>> StateStackPool { get; } =
            ThreadSafeObjectPool<Stack<RestoreState>>.Default;

        private Stack<RestoreState>? _states;

        internal DrawingContext()
        {
            
        }

        public void Dispose()
        {
            if (_states != null)
            {
                while (_states.Count > 0)
                    _states.Pop().Dispose();

                StateStackPool.ReturnAndSetNull(ref _states);
            }

            DisposeCore();
        }
        
        protected abstract void DisposeCore();
        
        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="source">The image.</param>
        /// <param name="rect">The rect in the output to draw to.</param>
        public virtual void DrawImage(IImage source, Rect rect)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            DrawImage(source, new Rect(source.Size), rect);
        }


        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="source">The image.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        public virtual void DrawImage(IImage source, Rect sourceRect, Rect destRect)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            source.Draw(this, sourceRect, destRect);
        }
        
        /// <summary>
        /// Draws a platform-specific bitmap impl.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacity">The opacity to draw with.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        internal abstract void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect);

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p2">The second point of the line.</param>
        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            if (PenIsVisible(pen))
                DrawLineCore(pen, p1, p2);
        }

        protected abstract void DrawLineCore(IPen pen, Point p1, Point p2);

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush? brush, IPen? pen, Geometry geometry)
        {
            if ((brush != null || PenIsVisible(pen)) && geometry.PlatformImpl != null)
                DrawGeometryCore(brush, pen, geometry.PlatformImpl);
        }
        
        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
        {
            if ((brush != null || PenIsVisible(pen)))
                DrawGeometryCore(brush, pen, geometry);
        }

        protected abstract void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry);

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
        /// If the pen is null, then no stroke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        public void DrawRectangle(IBrush? brush, IPen? pen, Rect rect,
            double radiusX = 0, double radiusY = 0,
            BoxShadows boxShadows = default)
        {
            if (brush == null && !PenIsVisible(pen) && boxShadows.Count == 0)
                return;
            if (!MathUtilities.IsZero(radiusX))
            {
                radiusX = Math.Min(radiusX, rect.Width / 2);
            }

            if (!MathUtilities.IsZero(radiusY))
            {
                radiusY = Math.Min(radiusY, rect.Height / 2);
            }
            
            DrawRectangleCore(brush, pen, new RoundedRect(rect, radiusX, radiusY), boxShadows);
        }
        
        /// <summary>
        /// Draws a rectangle with the specified Brush and Pen.
        /// </summary>
        /// <param name="brush">The brush used to fill the rectangle, or <c>null</c> for no fill.</param>
        /// <param name="pen">The pen used to stroke the rectangle, or <c>null</c> for no stroke.</param>
        /// <param name="rrect">The rectangle bounds.</param>
        /// <param name="boxShadows">Box shadow effect parameters</param>
        /// <remarks>
        /// The brush and the pen can both be null. If the brush is null, then no fill is performed.
        /// If the pen is null, then no stoke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default)
        {
            if (brush == null && !PenIsVisible(pen) && boxShadows.Count == 0)
                return;
            DrawRectangleCore(brush, pen, rrect, boxShadows);
        }

        protected abstract void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect,
            BoxShadows boxShadows = default);
        
        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void DrawRectangle(IPen pen, Rect rect, float cornerRadius = 0.0f) =>
            DrawRectangle(null, pen, rect, cornerRadius, cornerRadius);

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0.0f) => 
            DrawRectangle(brush, null, rect, cornerRadius, cornerRadius);

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
        public void DrawEllipse(IBrush? brush, IPen? pen, Point center, double radiusX, double radiusY)
        {
            if (brush != null || PenIsVisible(pen))
            {
                var originX = center.X - radiusX;
                var originY = center.Y - radiusY;
                var width = radiusX * 2;
                var height = radiusY * 2;
                DrawEllipseCore(brush, pen, new Rect(originX, originY, width, height));
            }
        }        
        
        /// <summary>
        /// Draws an ellipse with the specified Brush and Pen.
        /// </summary>
        /// <param name="brush">The brush used to fill the ellipse, or <c>null</c> for no fill.</param>
        /// <param name="pen">The pen used to stroke the ellipse, or <c>null</c> for no stroke.</param>
        /// <param name="rect">The bounding rect.</param>
        /// <remarks>
        /// The brush and the pen can both be null. If the brush is null, then no fill is performed.
        /// If the pen is null, then no stoke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
        {
            if (brush != null || PenIsVisible(pen))
                DrawEllipseCore(brush, pen, rect);
        }

        protected abstract void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect);

        /// <summary>
        /// Draws a custom drawing operation
        /// </summary>
        /// <param name="custom">custom operation</param>
        public abstract void Custom(ICustomDrawOperation custom);

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public virtual void DrawText(FormattedText text, Point origin)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
         
            text.Draw(this, origin);            
        }

        /// <summary>
        /// Draws a glyph run.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="glyphRun">The glyph run.</param>
        public abstract void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun);

        public record struct PushedState : IDisposable
        {
            private readonly DrawingContext _context;
            private readonly int _level;

            public PushedState(DrawingContext context)
            {
                _context = context;
                _level = _context._states!.Count;
            }

            public void Dispose()
            {
                if(_context?._states == null)
                    return;
                if(_context._states.Count != _level)
                    throw new InvalidOperationException("Wrong Push/Pop state order");
                _context._states.Pop().Dispose();
            }
        }
        
        private readonly record struct RestoreState : IDisposable
        {
            private readonly DrawingContext _context;
            private readonly PushedStateType _type;

            public enum PushedStateType
            {
                None,
                Transform,
                Opacity,
                Clip,
                GeometryClip,
                OpacityMask,
                RenderOptions
            }

            public RestoreState(DrawingContext context, PushedStateType type)
            {
                _context = context;
                _type = type;
            }

            public void Dispose()
            {
                if (_type == PushedStateType.None)
                    return;
                if (_context._states is null)
                    throw new ObjectDisposedException(nameof(DrawingContext));
                if (_type == PushedStateType.Transform)
                    _context.PopTransformCore();
                else if (_type == PushedStateType.Clip)
                    _context.PopClipCore();
                else if (_type == PushedStateType.Opacity)
                    _context.PopOpacityCore();
                else if (_type == PushedStateType.GeometryClip)
                    _context.PopGeometryClipCore();
                else if (_type == PushedStateType.OpacityMask)
                    _context.PopOpacityMaskCore();
                else if (_type == PushedStateType.RenderOptions)
                    _context.PopRenderOptionsCore();
            }
        }

        /// <summary>
        /// Pushes a clip rectangle.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public PushedState PushClip(RoundedRect clip)
        {
            PushClipCore(clip);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.Clip));
            return new PushedState(this);
        }

        protected abstract void PushClipCore(RoundedRect rect);

        /// <summary>
        /// Pushes a clip rectangle.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public PushedState PushClip(Rect clip)
        {
            PushClipCore(clip);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.Clip));
            return new PushedState(this);
        }
        
        protected abstract void PushClipCore(Rect rect);

        /// <summary>
        /// Pushes a clip geometry.
        /// </summary>
        /// <param name="clip">The clip geometry.</param>
        /// <returns>A disposable used to undo the clip geometry.</returns>
        public PushedState PushGeometryClip(Geometry clip)
        {
            PushGeometryClipCore(clip);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.GeometryClip));
            return new PushedState(this);
        }
        
        protected abstract void PushGeometryClipCore(Geometry clip);

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public PushedState PushOpacity(double opacity)
        {
            PushOpacityCore(opacity);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.Opacity));
            return new PushedState(this);
        }
        protected abstract void PushOpacityCore(double opacity);

        /// <summary>
        /// Pushes an opacity mask.
        /// </summary>
        /// <param name="mask">The opacity mask.</param>
        /// <param name="bounds">
        /// The size of the brush's target area. TODO: Are we sure this is needed?
        /// </param>
        /// <returns>A disposable to undo the opacity mask.</returns>
        public PushedState PushOpacityMask(IBrush mask, Rect bounds)
        {
            PushOpacityMaskCore(mask, bounds);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.OpacityMask));
            return new PushedState(this);
        }
        protected abstract void PushOpacityMaskCore(IBrush mask, Rect bounds);

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushTransform(Matrix matrix)
        {
            PushTransformCore(matrix);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.Transform));
            return new PushedState(this);
        }

        /// <summary>
        /// Pushes render options.
        /// </summary>
        /// <param name="renderOptions">The render options.</param>
        /// <returns>A disposable to undo the render options.</returns>
        public PushedState PushRenderOptions(RenderOptions renderOptions)
        {
            PushRenderOptionsCore(renderOptions);
            _states ??= StateStackPool.Get();
            _states.Push(new RestoreState(this, RestoreState.PushedStateType.RenderOptions));
            return new PushedState(this);
        }
        protected abstract void PushRenderOptionsCore(RenderOptions renderOptions);

        [Obsolete("Use PushTransform"), EditorBrowsable(EditorBrowsableState.Never)]
        public PushedState PushPreTransform(Matrix matrix) => PushTransform(matrix);
        [Obsolete("Use PushTransform"), EditorBrowsable(EditorBrowsableState.Never)]
        public PushedState PushPostTransform(Matrix matrix) => PushTransform(matrix);
        [Obsolete("Use PushTransform"), EditorBrowsable(EditorBrowsableState.Never)]
        public PushedState PushTransformContainer() => PushTransform(Matrix.Identity);
        
        
        protected abstract void PushTransformCore(Matrix matrix);

        protected abstract void PopClipCore();
        protected abstract void PopGeometryClipCore();
        protected abstract void PopOpacityCore();
        protected abstract void PopOpacityMaskCore();
        protected abstract void PopTransformCore();
        protected abstract void PopRenderOptionsCore();
        
        private static bool PenIsVisible(IPen? pen)
        {
            return pen?.Brush != null && pen.Thickness > 0;
        }
    }
}
