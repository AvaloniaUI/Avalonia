using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using Perspex.Media.Imaging;

namespace Perspex.Media
{
    public class ValidatingDrawingContext : IDrawingContext
    {
        private readonly IDrawingContext _base;

        public ValidatingDrawingContext(IDrawingContext @base)
        {
            _base = @base;
        }

        public void Dispose()
        {
            _base.Dispose();
        }

        public Matrix CurrentTransform => _base.CurrentTransform;
        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            _base.DrawImage(source, opacity, sourceRect, destRect);
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            _base.DrawLine(pen, p1, p2);
        }

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            _base.DrawGeometry(brush, pen, geometry);
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            _base.DrawRectangle(pen, rect, cornerRadius);
        }

        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            _base.DrawText(foreground, origin, text);
        }

        public void FillRectangle(Brush brush, Rect rect, float cornerRadius = 0)
        {
            _base.FillRectangle(brush, rect, cornerRadius);
        }


        Stack<IDisposable> _stateStack = new Stack<IDisposable>();

        IDisposable Transform(IDisposable disposable)
        {
            _stateStack.Push(disposable);
            return Disposable.Create(() =>
            {
                var current = _stateStack.Peek();
                if (current != disposable)
                    throw new InvalidOperationException("Invalid push/pop order");
                current.Dispose();
                _stateStack.Pop();
            });
        }

        public IDisposable PushClip(Rect clip) => Transform(_base.PushClip(clip));

        public IDisposable PushOpacity(double opacity) => Transform(_base.PushOpacity(opacity));

        public IDisposable PushTransform(Matrix matrix) => Transform(_base.PushTransform(matrix));
    }
}