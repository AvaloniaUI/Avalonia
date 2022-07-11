using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.NativeGraphics.Backend
{
    class AvgDrawingContext : IDrawingContextImpl, IDrawingContextWithAcrylicLikeSupport
    {
        private readonly IAvgDrawingContext _native;
        private Matrix _transform;
        private readonly Matrix? _postTransform;
        
        private PlatformRenderInterface _renderInterface;

        public AvgDrawingContext(IAvgDrawingContext native)
        {
            var renderInterace = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            _renderInterface = (PlatformRenderInterface) renderInterace;
            _native = native;

            _postTransform =
                Matrix.CreateScale(_native.Scaling, _native.Scaling);

        }

        public void Dispose() => _native.Dispose();

        public void Clear(Color color)
        {
            _native.Clear(color.ToUint32());
        }
        
        public void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
        {
            ImmutableBitmap bmp = (ImmutableBitmap) source.Item;
            
            _native.DrawImage(bmp._image);
        }

        public void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            
        }

        public void DrawImage(IAvgImage source)
        {
           _native.DrawImage(source); 
        }

        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            _native.DrawLine(new AvgPoint { X = p1.X, Y = p1.Y}, new AvgPoint{X = p2.X, Y = p2.Y}, CreateAvgPen(pen));
        }

        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            var impl = (GeometryImpl) geometry;
            _native.DrawGeometry(impl._avgPath, CreateAvgBrush(brush), CreateAvgPen(pen));
        }

        public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
        {
            if (rect.Rect.Height <= 0 || rect.Rect.Width <= 0)
                return;
            
            AvgRoundRect avgRoundRect = rect.ToAvgRoundRect();

            if (material != null)
            {
                Brush b = new SolidColorBrush(material.TintColor);
                unsafe
                {
                    _native.DrawRectangle(avgRoundRect, CreateAvgBrush(b), CreateAvgPen(null), null, 0);
                }
            }
        }

        private AvgBrush CreateAvgBrush(IBrush brush)
        {
            if (brush == null)
            {
                return new AvgBrush {Valid = 0};
            }

           return brush.ToAvgBrush();
        }

        private AvgPen CreateAvgPen(IPen pen)
        {
            if (pen == null)
            {
                return new AvgPen {Valid = 0};
            }

            return pen.ToAvgPen();
        }
        
        public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect, BoxShadows boxShadows = new BoxShadows())
        {
            AvgRoundRect avgRoundRect = rect.ToAvgRoundRect();

            AvgBoxShadow[] bs = new AvgBoxShadow[boxShadows.Count];
            int idx = 0;
            foreach (var boxShadow in boxShadows)
            {
                bs[idx++] = new AvgBoxShadow
                {
                    OffsetX = boxShadow.OffsetX,
                    OffsetY = boxShadow.OffsetY,
                    Blur = boxShadow.Blur,
                    Spread = boxShadow.Spread,
                    color = (int) boxShadow.Color.ToUint32(),
                    IsInset = Convert.ToInt32(boxShadow.IsInset),
                };
            }
            
            unsafe
            {
                fixed (AvgBoxShadow* b = bs)
                {
                    _native.DrawRectangle(avgRoundRect, CreateAvgBrush(brush), CreateAvgPen(pen), b, boxShadows.Count);
                }
            }
        }
        
        public void DrawEllipse(IBrush brush, IPen pen, Rect rect)
        {
            throw new NotImplementedException();
        }

        public void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
        {
            //Console.WriteLine($"Draw Glyphrun! - {glyphRun.GlyphRunImpl}");
            GlyphRunImpl glyphRunImpl = (GlyphRunImpl) glyphRun.GlyphRunImpl;
            _native.DrawGlyphRun(glyphRunImpl.AvgGlyphRun, glyphRun.BaselineOrigin.X, glyphRun.BaselineOrigin.Y, foreground.ToAvgBrush());
        }

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            throw new NotImplementedException();
        }

        public void PushClip(Rect clip)
        {
           PushClip(new RoundedRect(clip));
        }

        public void PushClip(RoundedRect clip)
        {
            var avgRect = clip.ToAvgRoundRect();
            _native.PushClip(avgRect); 
        }

        public void PopClip()
        {
            _native.PopClip();
        }

        public void PushOpacity(double opacity)
        {
            _native.PushOpacity(opacity);
        }

        public void PopOpacity()
        {
            _native.PopOpacity();
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
                var transform = value;
                
                if (_postTransform.HasValue)
                {
                    transform *= _postTransform.Value;
                }

                _transform = value;
                
                var m = new AvgMatrix3x2
                {
                    M11 = transform.M11,
                    M12 = transform.M12,
                    M21 = transform.M21,
                    M22 = transform.M22,
                    M31 = transform.M31,
                    M32 = transform.M32,
                };
                
                _native.SetTransform(&m);
            }
        }
    }
}
