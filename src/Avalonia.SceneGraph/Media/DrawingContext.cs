using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    public sealed class DrawingContext : IDisposable
    {
        private readonly IDrawingContextImpl _impl;
        private int _currentLevel;
        //Internal tranformation that is applied but not exposed anywhere
        //To be used for DPI scaling, etc
        private Matrix? _hiddenPostTransform = Matrix.Identity;

        

        static readonly Stack<Stack<PushedState>> StateStackPool = new Stack<Stack<PushedState>>();
        static readonly Stack<Stack<TransformContainer>> TransformStackPool = new Stack<Stack<TransformContainer>>();

        private Stack<PushedState> _states = StateStackPool.Count == 0 ? new Stack<PushedState>() : StateStackPool.Pop();

        private Stack<TransformContainer> _transformContainers = TransformStackPool.Count == 0
            ? new Stack<TransformContainer>()
            : TransformStackPool.Pop();

        struct TransformContainer
        {
            public readonly Matrix LocalTransform;
            public readonly Matrix ContainerTransform;

            public TransformContainer(Matrix localTransform, Matrix containerTransform)
            {
                LocalTransform = localTransform;
                ContainerTransform = containerTransform;
            }
        }

        public DrawingContext(IDrawingContextImpl impl, Matrix? hiddenPostTransform = null)
        {
            _impl = impl;
            _hiddenPostTransform = hiddenPostTransform;
        }


        private Matrix _currentTransform = Matrix.Identity;

        private Matrix _currentContainerTransform = Matrix.Identity;
        
        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get { return _currentTransform; }
            private set
            {
                _currentTransform = value;
                var transform = _currentTransform*_currentContainerTransform;
                if (_hiddenPostTransform.HasValue)
                    transform = transform*_hiddenPostTransform.Value;
                _impl.Transform = transform;
            }
        }

        //HACK: This is a temporary hack that is used in the render loop 
        //to update TransformedBounds property
        [Obsolete("HACK for render loop, don't use")]
        internal Matrix CurrentContainerTransform => _currentContainerTransform;        

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacity">The opacity to draw with.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
            => _impl.DrawImage(source, opacity, sourceRect, destRect);

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p2">The second point of the line.</param>
        public void DrawLine(Pen pen, Point p1, Point p2) => _impl.DrawLine(pen, p1, p2);

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush brush, Pen pen, Geometry geometry) => _impl.DrawGeometry(brush, pen, geometry);

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0.0f)
            => _impl.DrawRectangle(pen, rect, cornerRadius);

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public void DrawText(IBrush foreground, Point origin, FormattedText text)
            => _impl.DrawText(foreground, origin, text);

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0.0f)
            => _impl.FillRectangle(brush, rect, cornerRadius);

        public struct PushedState : IDisposable
        {
            private readonly int _level;
            private readonly DrawingContext _context;
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
                OpacityMask
            }

            public PushedState(DrawingContext context, PushedStateType type, Matrix matrix = default(Matrix))
            {
                _context = context;
                _type = type;
                _matrix = matrix;
                _level = context._currentLevel += 1;
                context._states.Push(this);
            }

            public void Dispose()
            {
                if(_type == PushedStateType.None)
                    return;
                if (_context._currentLevel != _level)
                    throw new InvalidOperationException("Wrong Push/Pop state order");
                _context._currentLevel--;
                _context._states.Pop();
                if (_type == PushedStateType.Matrix)
                    _context.CurrentTransform = _matrix;
                else if (_type == PushedStateType.Clip)
                    _context._impl.PopClip();
                else if (_type == PushedStateType.Opacity)
                    _context._impl.PopOpacity();
                else if (_type == PushedStateType.GeometryClip)
                    _context._impl.PopGeometryClip();
                else if (_type == PushedStateType.OpacityMask)
                    _context._impl.PopOpacityMask();
                else if (_type == PushedStateType.MatrixContainer)
                {
                    var cont = _context._transformContainers.Pop();
                    _context._currentContainerTransform = cont.ContainerTransform;
                    _context.CurrentTransform = cont.LocalTransform;
                }
            }
        }


        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public PushedState PushClip(Rect clip)
        {
            _impl.PushClip(clip);
            return new PushedState(this, PushedState.PushedStateType.Clip);
        }

        /// <summary>
        /// Pushes a clip geometry.
        /// </summary>
        /// <param name="clip">The clip geometry.</param>
        /// <returns>A disposable used to undo the clip geometry.</returns>
        public PushedState PushGeometryClip(Geometry clip)
        {
            Contract.Requires<ArgumentNullException>(clip != null);
            _impl.PushGeometryClip(clip);
            return new PushedState(this, PushedState.PushedStateType.GeometryClip);
        }

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public PushedState PushOpacity(double opacity)
            //TODO: Eliminate platform-specific push opacity call
        {
            _impl.PushOpacity(opacity);
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
        public PushedState PushOpacityMask(IBrush mask, Rect bounds)
        {
            _impl.PushOpacityMask(mask, bounds);
            return new PushedState(this, PushedState.PushedStateType.OpacityMask);
        }

        /// <summary>
        /// Pushes a matrix post-transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPostTransform(Matrix matrix) => PushSetTransform(CurrentTransform*matrix);

        /// <summary>
        /// Pushes a matrix pre-transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPreTransform(Matrix matrix) => PushSetTransform(matrix*CurrentTransform);

        /// <summary>
        /// Sets the current matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        PushedState PushSetTransform(Matrix matrix)
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
            _transformContainers.Push(new TransformContainer(CurrentTransform, _currentContainerTransform));
            _currentContainerTransform = CurrentTransform*_currentContainerTransform;
            _currentTransform = Matrix.Identity;
            return new PushedState(this, PushedState.PushedStateType.MatrixContainer);
        }

        /// <summary>
        /// Disposes of any resources held by the <see cref="DrawingContext"/>.
        /// </summary>
        public void Dispose()
        {
            while (_states.Count != 0)
                _states.Peek().Dispose();
            StateStackPool.Push(_states);
            _states = null;
            TransformStackPool.Push(_transformContainers);
            _transformContainers = null;
            _impl.Dispose();
        }
    }
}
