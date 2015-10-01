using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Media;
using Perspex.Media.Imaging;

namespace Perspex.Android.Rendering
{
    public class DrawingContext : IDrawingContext
    {
        private PerspexActivity _nativeContext;

        public DrawingContext()
        {
           _nativeContext = (PerspexActivity) global::Android.App.Application.Context;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Matrix CurrentTransform
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            throw new NotImplementedException();
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            
            //_nativeContext.PerspexView.Draw();
        }

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            throw new NotImplementedException();
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            throw new NotImplementedException();
        }

        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            throw new NotImplementedException();
        }

        public void FillRectangle(Brush brush, Rect rect, float cornerRadius = 0)
        {
            throw new NotImplementedException();
        }

        public IDisposable PushClip(Rect clip)
        {
            throw new NotImplementedException();
        }

        public IDisposable PushOpacity(double opacity)
        {
            throw new NotImplementedException();
        }

        public IDisposable PushTransform(Matrix matrix)
        {
            throw new NotImplementedException();
        }
    }
}