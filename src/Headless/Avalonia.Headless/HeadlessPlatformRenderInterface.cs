using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Headless
{
    internal class HeadlessPlatformRenderInterface : IPlatformRenderInterface, IPlatformRenderInterfaceContext
    {
        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(new HeadlessPlatformRenderInterface())
                .Bind<IFontManagerImpl>().ToConstant(new HeadlessFontManagerStub())
                .Bind<ITextShaperImpl>().ToConstant(new HeadlessTextShaperStub());
        }

        public IEnumerable<string> InstalledFontNames { get; } = new[] { "Tahoma" };

        public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext? graphicsContext) => this;

        public bool SupportsIndividualRoundRects => false;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Rgba8888;
        public bool IsSupportedBitmapPixelFormat(PixelFormat format) => true;

        public IGeometryImpl CreateEllipseGeometry(Rect rect) => new HeadlessGeometryStub(rect);

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            var tl = new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var br = new Point(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));
            return new HeadlessGeometryStub(new Rect(tl, br));
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return new HeadlessGeometryStub(rect);
        }

        public IStreamGeometryImpl CreateStreamGeometry() => new HeadlessStreamingGeometryStub();

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children) =>
            new HeadlessGeometryStub(children.Count != 0 ?
                children.Select(c => c.Bounds).Aggregate((a, b) => a.Union(b)) :
                default);

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2) 
            => new HeadlessGeometryStub(g1.Bounds.Union(g2.Bounds));

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces) => new HeadlessRenderTarget();
        public bool IsLost => false;
        public IReadOnlyDictionary<Type, object> PublicFeatures { get; } = new Dictionary<Type, object>();
        public object? TryGetFeature(Type featureType) => null;

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return new HeadlessBitmapStub(size, dpi);
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new HeadlessBitmapStub(size, dpi);
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new HeadlessBitmapStub(new Size(1, 1), new Vector(96, 96));
        }        

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(new Size(width, width), new Vector(96, 96));
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(new Size(height, height), new Vector(96, 96));
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new HeadlessBitmapStub(destinationSize, new Vector(96, 96));
        }

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            return new HeadlessGeometryStub(glyphRun.Bounds);
        }

        public IGlyphRunImpl CreateGlyphRun(
            IGlyphTypeface glyphTypeface, 
            double fontRenderingEmSize,
            IReadOnlyList<GlyphInfo> glyphInfos, 
            Point baselineOrigin)
        {
            return new HeadlessGlyphRunStub(glyphTypeface, fontRenderingEmSize, baselineOrigin);
        }

        internal class HeadlessGlyphRunStub : IGlyphRunImpl
        {
            public HeadlessGlyphRunStub(
                IGlyphTypeface glyphTypeface,
                double fontRenderingEmSize,
                Point baselineOrigin)
            {
                GlyphTypeface = glyphTypeface;
                FontRenderingEmSize = fontRenderingEmSize;
                BaselineOrigin = baselineOrigin;
            }

            public Rect Bounds { get; }

            public Point BaselineOrigin { get; }

            public IGlyphTypeface GlyphTypeface { get; }

            public double FontRenderingEmSize { get; }           

            public void Dispose()
            {
            }

            public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
                => Array.Empty<float>();
        }

        private class HeadlessGeometryStub : IGeometryImpl
        {
            public HeadlessGeometryStub(Rect bounds)
            {
                Bounds = bounds;
            }

            public Rect Bounds { get; set; }
            
            public double ContourLength { get; } = 0;
            
            public virtual bool FillContains(Point point) => Bounds.Contains(point);

            public Rect GetRenderBounds(IPen? pen)
            {
                if(pen is null)
                {
                    return Bounds;
                }

                return Bounds.Inflate(pen.Thickness / 2);
            }

            public IGeometryImpl GetWidenedGeometry(IPen pen) => this;

            public bool StrokeContains(IPen? pen, Point point)
            {
                return false;
            }

            public IGeometryImpl Intersect(IGeometryImpl geometry)
                => new HeadlessGeometryStub(geometry.Bounds.Intersect(Bounds));

            public ITransformedGeometryImpl WithTransform(Matrix transform) =>
                new HeadlessTransformedGeometryStub(this, transform);

            public bool TryGetPointAtDistance(double distance, out Point point)
            {
                point = new Point();
                return false;
            }

            public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
            {
                point = new Point();
                tangent = new Point();
                return false;
            }

            public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, [NotNullWhen(true)] out IGeometryImpl? segmentGeometry)
            {
                segmentGeometry = null;
                return false;
            }
        }

        private class HeadlessTransformedGeometryStub : HeadlessGeometryStub, ITransformedGeometryImpl
        {
            public HeadlessTransformedGeometryStub(IGeometryImpl b, Matrix transform) : this(Fix(b, transform))
            {

            }

            private static (IGeometryImpl, Matrix, Rect) Fix(IGeometryImpl b, Matrix transform)
            {
                if (b is HeadlessTransformedGeometryStub transformed)
                {
                    b = transformed.SourceGeometry;
                    transform = transformed.Transform * transform;
                }

                return (b, transform, b.Bounds.TransformToAABB(transform));
            }

            private HeadlessTransformedGeometryStub((IGeometryImpl b, Matrix transform, Rect bounds) fix) : base(fix.bounds)
            {
                SourceGeometry = fix.b;
                Transform = fix.transform;
            }


            public IGeometryImpl SourceGeometry { get; }
            public Matrix Transform { get; }
        }

        private class HeadlessStreamingGeometryStub : HeadlessGeometryStub, IStreamGeometryImpl
        {
            private HeadlessStreamingGeometryContextStub _context;
            
            public HeadlessStreamingGeometryStub() : base(default)
            {
                _context = new HeadlessStreamingGeometryContextStub(this);
            }

            public IStreamGeometryImpl Clone()
            {
                return this;
            }

            public IStreamGeometryContextImpl Open()
            {
                return _context;
            }

            public override bool FillContains(Point point)
            {
                return _context.FillContains(point);
            }

            private class HeadlessStreamingGeometryContextStub : IStreamGeometryContextImpl
            {
                private readonly HeadlessStreamingGeometryStub _parent;
                private List<Point> points = new List<Point>();
                public HeadlessStreamingGeometryContextStub(HeadlessStreamingGeometryStub parent)
                {
                    _parent = parent;
                }

                private void Track(Point pt)
                {
                    points.Add(pt);
                }

                public Rect CalculateBounds()
                {
                    var left = double.MaxValue;
                    var right = double.MinValue;
                    var top = double.MaxValue;
                    var bottom = double.MinValue;

                    foreach (var p in points)
                    {
                        left = Math.Min(p.X, left);
                        right = Math.Max(p.X, right);
                        top = Math.Min(p.Y, top);
                        bottom = Math.Max(p.Y, bottom);
                    }

                    return new Rect(new Point(left, top), new Point(right, bottom));
                }
                
                public void Dispose()
                {
                    _parent.Bounds = CalculateBounds();
                }

                public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
                    => Track(point);

                public void BeginFigure(Point startPoint, bool isFilled = true) => Track(startPoint);

                public void CubicBezierTo(Point point1, Point point2, Point point3)
                {
                    Track(point1);
                    Track(point2);
                    Track(point3);
                }

                public void QuadraticBezierTo(Point control, Point endPoint)
                {
                    Track(control);
                    Track(endPoint);
                }

                public void LineTo(Point point) => Track(point);

                public void EndFigure(bool isClosed)
                {
                    Dispose();
                }

                public void SetFillRule(FillRule fillRule)
                {

                }
                
                public bool FillContains(Point point)
                {
                    // Use the algorithm from https://www.blackpawn.com/texts/pointinpoly/default.html
                    // to determine if the point is in the geometry (since it will always be convex in this situation)
                    for (int i = 0; i < points.Count; i++)
                    {
                        var a = points[i];
                        var b = points[(i + 1) % points.Count];
                        var c = points[(i + 2) % points.Count];

                        Vector v0 = c - a;
                        Vector v1 = b - a;
                        Vector v2 = point - a;

                        var dot00 = v0 * v0;
                        var dot01 = v0 * v1;
                        var dot02 = v0 * v2;
                        var dot11 = v1 * v1;
                        var dot12 = v1 * v2;


                        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
                        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
                        if ((u >= 0) && (v >= 0) && (u + v < 1)) return true;
                    }
                    return false;
                }
            }
        }

        private class HeadlessBitmapStub : IBitmapImpl, IDrawingContextLayerImpl, IWriteableBitmapImpl
        {
            public Size Size { get; }

            public HeadlessBitmapStub(Size size, Vector dpi)
            {
                Size = size;
                Dpi = dpi;
                var pixel = Size * (Dpi / 96);
                PixelSize = new PixelSize(Math.Max(1, (int)pixel.Width), Math.Max(1, (int)pixel.Height));
            }

            public HeadlessBitmapStub(PixelSize size, Vector dpi)
            {
                PixelSize = size;
                Dpi = dpi;
                Size = PixelSize.ToSizeWithDpi(dpi);
            }

            public void Dispose()
            {

            }

            public IDrawingContextImpl CreateDrawingContext()
            {
                return new HeadlessDrawingContextStub();
            }

            public bool IsCorrupted => false;

            public void Blit(IDrawingContextImpl context)
            {
                
            }

            public bool CanBlit => false;

            public Vector Dpi { get; }
            public PixelSize PixelSize { get; }
            public PixelFormat? Format { get; }
            public AlphaFormat? AlphaFormat { get; }
            public int Version { get; set; }

            public void Save(string fileName, int? quality = null)
            {

            }

            public void Save(Stream stream, int? quality = null)
            {

            }


            public ILockedFramebuffer Lock()
            {
                Version++;
                var mem = Marshal.AllocHGlobal(PixelSize.Width * PixelSize.Height * 4);
                return new LockedFramebuffer(mem, PixelSize, PixelSize.Width * 4, Dpi, PixelFormat.Rgba8888,
                    () => Marshal.FreeHGlobal(mem));
            }
        }

        internal class HeadlessDrawingContextStub : IDrawingContextImpl
        {
            public void Dispose()
            {

            }

            public Matrix Transform { get; set; }

            public RenderOptions RenderOptions { get; set; }

            public void Clear(Color color)
            {

            }

            public IDrawingContextLayerImpl CreateLayer(Size size)
            {
                return new HeadlessBitmapStub(size, new Vector(96, 96));
            }

            public void PushClip(Rect clip)
            {

            }

            public void PopClip()
            {

            }

            public void PushOpacity(double opacity, Rect? rect)
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
            
            public object? GetFeature(Type t)
            {
                return null;
            }

            public void DrawLine(IPen? pen, Point p1, Point p2)
            {
            }

            public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
            {
            }

            public void DrawRectangle(IPen pen, Rect rect, float cornerRadius = 0)
            {
            }

            public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
            {
                
            }

            public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
            {
                
            }

            public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadow = default)
            {
                
            }

            public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
            {
            }

            public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
            {
                
            }

            public void PushClip(RoundedRect clip)
            {
                
            }

            public void PushRenderOptions(RenderOptions renderOptions)
            {
               
            }

            public void PopRenderOptions()
            {
               
            }
        }

        private class HeadlessRenderTarget : IRenderTarget
        {
            public void Dispose()
            {

            }

            public IDrawingContextImpl CreateDrawingContext()
            {
                return new HeadlessDrawingContextStub();
            }

            public bool IsCorrupted => false;
        }

        public void Dispose()
        {
            
        }
    }
}
