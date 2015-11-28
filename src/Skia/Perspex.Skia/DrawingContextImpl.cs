using System;
using System.Collections.Generic;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.RenderHelpers;

namespace Perspex.Skia
{
    unsafe class DrawingContextImpl : PerspexHandleHolder, IDrawingContextImpl
    {
        private readonly NativeDrawingContextSettings* _settings;
        public DrawingContextImpl(IntPtr handle) : base(handle)
        {
            _settings = MethodTable.Instance.GetDrawingContextSettingsPtr(handle);
            _settings->Opacity = 1;
        }

        protected override void Delete(IntPtr handle) => MethodTable.Instance.DisposeRenderingContext(handle);

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = (BitmapImpl) source.PlatformImpl;
            var s = SkRect.FromRect(sourceRect);
            var d = SkRect.FromRect(destRect);
            MethodTable.Instance.DrawImage(Handle, impl.Handle, (float) opacity, ref s, ref d);
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {

            using (var brush = CreateBrush(pen, new Size(Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y))))
                MethodTable.Instance.DrawLine(Handle, brush.Brush,
                    (float) p1.X, (float) p1.Y, (float) p2.X, (float) p2.Y);
        }

        static readonly NativeBrushContainer _dummy = new NativeBrushContainer(null);
        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            var impl = ((StreamGeometryImpl) geometry.PlatformImpl);
            var oldTransform = Transform;
            if (!impl.Transform.IsIdentity)
                Transform = impl.Transform*Transform;
            
            var size = geometry.Bounds.Size;
            using(var fill = brush!=null?CreateBrush(brush, size):null)
            using (var stroke = pen?.Brush != null ? CreateBrush(pen, size) : null)
            {
                MethodTable.Instance.DrawGeometry(Handle, impl.Path.Handle, fill != null ? fill.Brush : null,
                    stroke != null ? stroke.Brush : null);
            }
            Transform = oldTransform;
        }

        unsafe NativeBrushContainer CreateBrush(Brush brush, Size targetSize)
        {
            var rv = NativeBrushPool.Instance.Get();
            rv.Brush->Opacity = brush.Opacity;

            
            var solid = brush as SolidColorBrush;
            if (solid != null)
            {
                rv.Brush->Type = NativeBrushType.Solid;
                rv.Brush->Color = solid.Color.ToUint32();
                return rv;
            }
            var gradient = brush as GradientBrush;
            if (gradient != null)
            {
                if (gradient.GradientStops.Count > NativeBrush.MaxGradientStops)
                    throw new NotSupportedException("Maximum supported gradient stop count is " +
                                                    NativeBrush.MaxGradientStops);
                rv.Brush->GradientSpreadMethod = gradient.SpreadMethod;
                rv.Brush->GradientStopCount = gradient.GradientStops.Count;

                for (var c = 0; c < gradient.GradientStops.Count; c++)
                {
                    var st = gradient.GradientStops[c];
                    rv.Brush->GradientStops[c] = (float) st.Offset;
                    rv.Brush->GradientStopColors[c] = st.Color.ToUint32();
                }

            }

            var linearGradient = brush as LinearGradientBrush;
            if (linearGradient != null)
            {
                rv.Brush->Type = NativeBrushType.LinearGradient;
                rv.Brush->GradientStartPoint = linearGradient.StartPoint.ToPixels(targetSize);
                rv.Brush->GradientEndPoint = linearGradient.EndPoint.ToPixels(targetSize);
            }
            var radialGradient = brush as RadialGradientBrush;
            if (radialGradient != null)
            {
                rv.Brush->Type = NativeBrushType.RadialGradient;
                rv.Brush->GradientStartPoint = radialGradient.Center.ToPixels(targetSize);
                rv.Brush->GradientRadius = (float)radialGradient.Radius;
            }
            var tileBrush = brush as TileBrush;
            if (tileBrush != null)
            {
                rv.Brush->Type = NativeBrushType.Image;
                var helper = new TileBrushImplHelper(tileBrush, targetSize);
                var bitmap = new BitmapImpl((int) helper.IntermediateSize.Width, (int) helper.IntermediateSize.Height);
                rv.AddDisposable(bitmap);
                using (var ctx = bitmap.CreateDrawingContext())
                    helper.DrawIntermediate(ctx);
                rv.Brush->Bitmap = bitmap.Handle;
                rv.Brush->BitmapTileMode = tileBrush.TileMode;
                rv.Brush->BitmapTranslation = new SkiaPoint(-helper.DestinationRect.X, -helper.DestinationRect.Y);


            }

            return rv;
        }

        NativeBrushContainer CreateBrush(Pen pen, Size targetSize)
        {
            var brush = CreateBrush(pen.Brush, targetSize);
            brush.Brush->Stroke = true;
            brush.Brush->StrokeThickness = (float)pen.Thickness;
            brush.Brush->StrokeLineCap = pen.StartLineCap;
            brush.Brush->StrokeMiterLimit = (float)pen.MiterLimit;

            if (pen.DashStyle?.Dashes != null)
            {
                var dashes = pen.DashStyle.Dashes;
                if (dashes.Count > NativeBrush.MaxDashCount)
                    throw new NotSupportedException("Maximum supported dash count is " + NativeBrush.MaxDashCount);
                brush.Brush->StrokeDashCount = dashes.Count;
                for (int c = 0; c < dashes.Count; c++)
                    brush.Brush->StrokeDashes[c] = (float) dashes[c];
                brush.Brush->StrokeDashOffset = (float)pen.DashStyle.Offset;

            }


            return brush;
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            using (var brush = CreateBrush(pen, rect.Size))
            {
                var rc = SkRect.FromRect(rect);
                MethodTable.Instance.DrawRectangle(Handle, brush.Brush, ref rc, cornerRadius);
            }
        }

        public void FillRectangle(Brush pbrush, Rect rect, float cornerRadius = 0)
        {
            using (var brush = CreateBrush(pbrush, rect.Size))
            {
                var rc = SkRect.FromRect(rect);
                MethodTable.Instance.DrawRectangle(Handle, brush.Brush, ref rc, cornerRadius);
            }
        }

        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            using (var br = CreateBrush(foreground, text.Measure()))
                MethodTable.Instance.DrawFormattedText(Handle, br.Brush, ((FormattedTextImpl) text.PlatformImpl).Handle,
                    (float) origin.X, (float) origin.Y);
        }


        public void PushClip(Rect clip)
        {
            var rc = SkRect.FromRect(clip);
            MethodTable.Instance.PushClip(Handle, ref rc);
        }

        public void PopClip()
        {
            MethodTable.Instance.PopClip(Handle);
        }

        private readonly Stack<double> _opacityStack = new Stack<double>();

        public void PushOpacity(double opacity)
        {
            _opacityStack.Push(_settings->Opacity);
            _settings->Opacity *= opacity;
        }

        public void PopOpacity() => _settings->Opacity = _opacityStack.Pop();

        private Matrix _currentTransform = Matrix.Identity;
        private readonly float[] _fmatrix = new float[6];
        public Matrix Transform
        {
            get { return _currentTransform; }
            set
            {
                if(_currentTransform == value)
                    return;
                _currentTransform = value;
                _fmatrix[0] = (float)value.M11;
                _fmatrix[1] = (float)value.M21;
                _fmatrix[2] = (float)value.M31;

                _fmatrix[3] = (float)value.M12;
                _fmatrix[4] = (float)value.M22;
                _fmatrix[5] = (float)value.M32;
                MethodTable.Instance.SetTransform(Handle, _fmatrix);
            } 
        }
    }
}