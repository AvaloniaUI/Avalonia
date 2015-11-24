using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Media.Imaging;

namespace Perspex.Media
{
    public sealed class DrawingContext : IDisposable
    {
        private readonly IDrawingContextImpl _impl;
        private int _currentLevel;

        

        static readonly Stack<Stack<PushedState>> StateStackPool = new Stack<Stack<PushedState>>();
        static readonly Stack<Stack<TransformContainer>> TransformStackPool = new Stack<Stack<TransformContainer>>();

        private Stack<PushedState> _states = StateStackPool.Count == 0 ? new Stack<PushedState>() : StateStackPool.Pop();

        private Stack<TransformContainer> _transformContainers = TransformStackPool.Count == 0
            ? new Stack<TransformContainer>()
            : TransformStackPool.Pop();

        struct TransformContainer
        {
            public Matrix LocalTransform;
            public Matrix ContainerTransform;

            public TransformContainer(Matrix localTransform, Matrix containerTransform)
            {
                LocalTransform = localTransform;
                ContainerTransform = containerTransform;
            }
        }

        public DrawingContext(IDrawingContextImpl impl)
        {
            _impl = impl;
        }


        private Matrix _currentTransform = Matrix.Identity;

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get { return _currentTransform; }
            private set
            {
                _currentTransform = value;
                _impl.Transform = _currentTransform*_currentContainerTransform;
            }
        }

        private Matrix _currentContainerTransform = Matrix.Identity;

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
        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry) => _impl.DrawGeometry(brush, pen, geometry);

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
        public void DrawText(Brush foreground, Point origin, FormattedText text)
            => _impl.DrawText(foreground, origin, text);

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectangle(Brush brush, Rect rect, float cornerRadius = 0.0f)
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
                MatrixContainer
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
                else if(_type == PushedStateType.Clip)
                    _context._impl.PopClip();
                else if(_type == PushedStateType.Opacity)
                    _context._impl.PopOpacity();
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
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public PushedState PushOpacity(double opacity)
            //TODO: Elimintate platform-specific push opacity call
        {
            _impl.PushOpacity(opacity);
            return new PushedState(this, PushedState.PushedStateType.Opacity);
        }

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public PushedState PushPostTransform(Matrix matrix) => PushSetTransform(CurrentTransform*matrix);

        public PushedState PushPreTransform(Matrix matrix) => PushSetTransform(matrix*CurrentTransform);
        

        PushedState PushSetTransform(Matrix matrix)
        {
            var oldMatrix = CurrentTransform;
            CurrentTransform = matrix;
            
            return new PushedState(this, PushedState.PushedStateType.Matrix, oldMatrix);
        }


        public PushedState PushTransformContainer()
        {
            _transformContainers.Push(new TransformContainer(CurrentTransform, _currentContainerTransform));
            _currentContainerTransform = CurrentTransform*_currentContainerTransform;
            _currentTransform = Matrix.Identity;
            return new PushedState(this, PushedState.PushedStateType.MatrixContainer);
        }

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
