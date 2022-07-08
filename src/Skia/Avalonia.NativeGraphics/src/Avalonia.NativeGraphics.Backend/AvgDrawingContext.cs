using System;
using Avalonia.Media;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.NativeGraphics.Backend
{
    class AvgDrawingContext : IDrawingContextImpl
    {
        private readonly IAvgDrawingContext _native;
        private Matrix _transform;

        public AvgDrawingContext(IAvgDrawingContext native)
        {
            _native = native;
        }

        public void Dispose() => _native.Dispose();

        public void Clear(Color color)
        {
            _native.Clear(color.ToUint32());
        }

        public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
        {
            
        }

        public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            
        }

        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            
        }

        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            
        }

        static uint GetBrushColor(IBrush b)
        {
            if (b is ISolidColorBrush scb)
                return scb.Color.ToUint32();
            if (b is IGradientBrush gb)
                return gb.GradientStops[0].Color.ToUint32();
            return 0xffff00ff;
        }
        
        public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect, BoxShadows boxShadows = new BoxShadows())
        {
            if(brush == null)
                return;
            _native.FillRect(new AvgRect
            {
                X = rect.Rect.Left,
                Y = rect.Rect.Top,
                Width = rect.Rect.Width,
                Height = rect.Rect.Height
            }, GetBrushColor(brush));
        }

        public void DrawEllipse(IBrush brush, IPen pen, Rect rect)
        {
            throw new NotImplementedException();
        }

        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            
        }

        public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
        {
            
        }

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            throw new NotImplementedException();
        }

        public void PushClip(Rect clip)
        {
            
        }

        public void PushClip(RoundedRect clip)
        {
            
        }

        public void PopClip()
        {
            
        }

        public void PushOpacity(double opacity)
        {
            
        }

        public void PopOpacity()
        {
            
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            
        }

        public void PopOpacityMask()
        {
            
        }

        public void PushGeometryClip(IGeometryImpl clip)
        {
            
        }

        public void PopGeometryClip()
        {
            
        }

        public void PushBitmapBlendMode(BitmapBlendingMode blendingMode)
        {
            
        }

        public void PopBitmapBlendMode()
        {
            
        }

        public void Custom(ICustomDrawOperation custom)
        {
            
        }

        public unsafe Matrix Transform
        {
            get => _transform;
            set
            {
                _transform = value;
                var m = new AvgMatrix3x2
                {
                    M11 = value.M11,
                    M12 = value.M12,
                    M21 = value.M21,
                    M22 = value.M22,
                    M31 = value.M31,
                    M32 = value.M32,

                };
                _native.SetTransform(&m);
            }
        }
    }
}