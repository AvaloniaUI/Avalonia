using Android.Graphics;
using Android.Util;
using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Specific;
using Perspex.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using AColor = Android.Graphics.Color;

namespace Perspex.Android.CanvasRendering
{
    public enum BrushUsage
    {
        Fill,
        Stroke,
        Both
    };

    public interface INativePaintWrapper : IDisposable
    {
        Paint NativePaint { get; }

        void PushOpacity(double value);

        void PopOpacity();
    }

    public abstract class BrushImpl : IDisposable, INativePaintWrapper
    {
        public BrushImpl(Brush brush)
        {
            Brush = brush;
            Brush.PropertyChanged += Brush_PropertyChanged;
            Init();
        }

        protected virtual void Init()
        {
            Opacity = Brush.Opacity;
        }

        private void Brush_PropertyChanged(object sender, PerspexPropertyChangedEventArgs e)
        {
            _isDirty = true;
        }

        protected bool _isDirty = false;

        public Brush Brush { get; private set; }

        private double _oldOpacity = 1;

        public double Opacity { get; private set; }

        public virtual void Dispose()
        {
            Brush.PropertyChanged -= Brush_PropertyChanged;
            Brush = null;
            NativePaint?.Dispose();
        }

        private Paint _nativeBrush;

        public Paint NativePaint
        {
            get
            {
                if (_nativeBrush == null)
                {
                    _nativeBrush = new Paint(PaintFlags.AntiAlias);
                    _isDirty = true;
                }

                if (_isDirty)
                {
                    Apply();
                }

                return _nativeBrush;
            }
        }

        public Paint.Style Style { get; set; }

        public virtual void ApplyTo(Paint paint)
        {
            paint.Alpha = AndroidGraphicsExtensions.OpacityToAndroidAlfa(Opacity);
            paint.SetStyle(Style);
        }

        private void Apply()
        {
            _isDirty = false;
            ApplyTo(NativePaint);
        }

        private static readonly BrushImpl FallbackBrush = new SolidColorBrushImpl(new SolidColorBrush(Colors.Transparent));

        public static BrushImpl Create(Brush brush, Size dstRect, Paint.Style style)
        {
            //Size dstRect = PointService.Instance.PerspexToNative(pdstRect);
            BrushImpl impl = null;

            if (brush is SolidColorBrush)
            {
                impl = new SolidColorBrushImpl(brush as SolidColorBrush);
            }
            else if (brush is LinearGradientBrush)
            {
                impl = new LinearGradientBrushImpl(brush as LinearGradientBrush, dstRect);
            }
            else if (brush is RadialGradientBrush)
            {
                impl = new RadialGradientBrushImpl(brush as RadialGradientBrush, dstRect);
            }
            else if (brush is ImageBrush)
            {
                impl = new ImageBrushImpl(brush as ImageBrush);
            }
            else if (brush is VisualBrush)
            {
                // TODO: Implement me
                Log.Debug("REND", "VisualBrush not implemented");
                impl = FallbackBrush;
            }
            else
            {
                impl = new SolidColorBrushImpl(null);
            }

            impl.Style = style;

            return impl;
        }

        private Stack<double> _opacityStack = new Stack<double>();

        public void PushOpacity(double value)
        {
            _opacityStack.Push(this.Opacity);
            Opacity = Opacity * value;
            _isDirty = value != 1 ? true : false;
        }

        public void PopOpacity()
        {
            double newOpacity = _opacityStack.Pop();
            if (newOpacity != Opacity)
            {
                Opacity = newOpacity;
                _isDirty = true;
            }
        }
    }

    public class PenImpl : IDisposable, INativePaintWrapper
    {
        public PenImpl(Pen pen, Size dstRect, Paint.Style style)
        {
            this.Pen = pen;
            if (this.Pen.Brush != null)
            {
                this.Brush = BrushImpl.Create(this.Pen.Brush, dstRect, style);
            }
        }

        private BrushImpl Brush { get; set; }

        public Pen Pen { get; }

        private Paint _nativePaint;

        public Paint NativePaint
        {
            get
            {
                if (_nativePaint == null)
                {
                    _nativePaint = new Paint(PaintFlags.AntiAlias);
                    Apply();
                }

                return _nativePaint;
            }
            set
            {
                this._nativePaint = value;
            }
        }

        public void PushOpacity(double opacity)
        {
            Brush?.PushOpacity(opacity);
        }

        public void PopOpacity()
        {
            Brush?.PopOpacity();
        }

        private void Apply()
        {
            if (Pen.DashStyle?.Dashes != null && Pen.DashStyle.Dashes.Count > 0)
            {
                var cray = Pen.DashStyle.Dashes.Select(d => (float)d).ToArray();
                NativePaint.SetPathEffect(new DashPathEffect(cray, (float)Pen.DashStyle.Offset));
            }

            NativePaint.StrokeWidth = PointUnitService.Instance.PerspexToNativeXF(Pen.Thickness);
            NativePaint.StrokeMiter = (float)Pen.MiterLimit;

            NativePaint.StrokeJoin = Pen.LineJoin.ToAndroidGraphics();
            NativePaint.StrokeCap = Pen.StartLineCap.ToAndroidGraphics();

            if (Brush != null)
            {
                Brush.ApplyTo(NativePaint);
            }
        }

        public void Dispose()
        {
            Brush?.Dispose();
        }
    }

    public class SolidColorBrushImpl : BrushImpl
    {
        private AColor _nativeColor;

        public SolidColorBrushImpl(SolidColorBrush brush) : base(brush)
        {
        }

        public override void ApplyTo(Paint paint)
        {
            paint.Color = _nativeColor;
            base.ApplyTo(paint);
        }

        protected override void Init()
        {
            base.Init();
            var brush = Brush as SolidColorBrush;

            _nativeColor = brush.Color.ToAndroidGraphics();
        }
    }

    public class ShaderBrushImpl : BrushImpl
    {
        protected Shader Shader { get; set; }

        protected Size DestinationSize { get; }

        public ShaderBrushImpl(Brush brush, Size destinationSize) : base(brush)
        {
            DestinationSize = destinationSize;
        }

        public override void ApplyTo(Paint paint)
        {
            base.ApplyTo(paint);

            if (Shader != null)
            {
                paint.SetShader(Shader);
            }
            else
            {
                //we had some problem better set something
                paint.Color = new AColor(0, 0, 0, 255);
            }
        }

        public override void Dispose()
        {
            Shader?.Dispose();
            base.Dispose();
        }
    }

    public class LinearGradientBrushImpl : ShaderBrushImpl
    {
        public LinearGradientBrushImpl(LinearGradientBrush brush, Size destinationSize) : base(brush, destinationSize)
        {
        }

        protected override void Init()
        {
            base.Init();
            var brush = Brush as LinearGradientBrush;
            int n = brush.GradientStops.Count;
            var offsets = new float[n];
            var colors = new int[n];
            for (var i = 0; i < n; i++)
            {
                var s = brush.GradientStops[i];
                offsets[i] = (float)s.Offset;
                colors[i] = (int)s.Color.ToUint32();
            }

            var startPoint = brush.StartPoint.ToPixels(DestinationSize);
            var endPoint = brush.EndPoint.ToPixels(DestinationSize);

            this.Shader = new LinearGradient(
              (float)startPoint.X, (float)startPoint.Y,
              (float)endPoint.X, (float)endPoint.Y,
              colors,
              offsets,
              Shader.TileMode.Clamp);
        }
    }

    public class RadialGradientBrushImpl : ShaderBrushImpl
    {
        public RadialGradientBrushImpl(RadialGradientBrush brush, Size destinationSize) : base(brush, destinationSize)
        {
        }

        protected override void Init()
        {
            base.Init();
            var brush = Brush as RadialGradientBrush;
            int n = brush.GradientStops.Count;
            var offsets = new float[n];
            var colors = new int[n];

            for (var i = 0; i < n; i++)
            {
                var s = brush.GradientStops[i];
                offsets[i] = (float)s.Offset;
                colors[i] = (int)s.Color.ToUint32();
            }

            var centerPoint = brush.Center.ToPixels(DestinationSize);
            double radius = brush.Center.Unit == RelativeUnit.Absolute ? brush.Radius : Math.Max(brush.Radius * DestinationSize.Width, brush.Radius * DestinationSize.Height);

            Shader = new RadialGradient(
              (float)centerPoint.X, (float)centerPoint.Y,
              (float)brush.Radius,
              colors,
              offsets,
              Shader.TileMode.Clamp);
        }
    }

    public class ImageBrushImpl : ShaderBrushImpl
    {
        public ImageBrushImpl(ImageBrush brush) : base(brush, default(Size))
        {
        }

        protected override void Init()
        {
            base.Init();
            var brush = Brush as ImageBrush;
            var bmp = brush.Source.PlatformImpl as BitmapImpl;
            Shader = new BitmapShader(bmp.PlatformBitmap, brush.TileMode.ToAndroidGraphicsX(), brush.TileMode.ToAndroidGraphicsY());
        }
    }
}