using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;

namespace Perspex.Skia
{
    internal enum NativeBrushType
    {
        Solid,
        LinearGradient,
        RadialGradient,
        Image
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeBrush
    {
        public const int MaxGradientStops = 1024;
        public const int MaxDashCount = 1024;
        public NativeBrushType Type;
        public double Opacity;
        public uint Color;

        //Strokes
        public bool Stroke;
        public float StrokeThickness;
        public PenLineJoin StrokeLineJoin;
        public float StrokeMiterLimit;
        public int StrokeDashCount;
        public float StrokeDashOffset;
        public PenLineCap StrokeLineCap;


        //Gradients
        public int GradientStopCount;
        public GradientSpreadMethod GradientSpreadMethod;
        public SkiaPoint GradientStartPoint, GradientEndPoint;
        public float GradientRadius;

        //Image Brush
        public IntPtr Bitmap;
        public TileMode BitmapTileMode;
        public SkiaPoint BitmapTranslation;

        //Blobs
        public fixed uint GradientStopColors [MaxGradientStops];
        public fixed float GradientStops [MaxGradientStops];
        public fixed float StrokeDashes [MaxDashCount];

        public void Reset()
        {
            Type = NativeBrushType.Solid;
            Opacity = 1f;
            Color = 0;
            Stroke = false;
            StrokeThickness = 1;
            GradientStopCount = 0;
            StrokeDashCount = 0;
            StrokeLineCap = PenLineCap.Flat;
        }
        
    }


    unsafe class NativeBrushContainer : IDisposable
    {
        private readonly NativeBrushPool _pool;
        public NativeBrush* Brush;

        readonly List<IDisposable> _disposables = new List<IDisposable>();

        public NativeBrushContainer(NativeBrushPool pool)
        {
            _pool = pool;
            Brush = (NativeBrush*) Marshal.AllocHGlobal(Marshal.SizeOf(typeof (NativeBrush))).ToPointer();
            Brush->Reset();
        }

        public void AddDisposable(IDisposable disp)
        {
            _disposables.Add(disp);
        }

        public void Dispose()
        {
            foreach (var disp in _disposables)
                disp.Dispose();
            _disposables.Clear();
            Brush->Reset();
            _pool?.Return(this);
        }
    }

    class NativeBrushPool
    {
        public static NativeBrushPool Instance { get; } = new NativeBrushPool();
        readonly Stack<NativeBrushContainer> _pool = new Stack<NativeBrushContainer>();

        public void Return(NativeBrushContainer c)
        {
            _pool.Push(c);
        }

        public NativeBrushContainer Get()
        {
            if (_pool.Count == 0)
                return new NativeBrushContainer(this);
            return _pool.Pop();
        }
    }
}
