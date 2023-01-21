using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Rendering;
using Moq;

namespace Avalonia.UnitTests
{
    public class MockPlatformRenderInterface : IPlatformRenderInterface, IPlatformRenderInterfaceContext
    {
        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return Mock.Of<IGeometryImpl>(x => x.Bounds == rect);
        }

        class MockRenderTarget : IRenderTarget
        {
            public void Dispose()
            {
                
            }

            public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            {
                var m = new Mock<IDrawingContextImpl>();
                m.Setup(c => c.CreateLayer(It.IsAny<Size>()))
                    .Returns(() =>
                        {
                            var r = new Mock<IDrawingContextLayerImpl>();
                            r.Setup(r => r.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>()))
                                .Returns(CreateDrawingContext(null));
                            return r.Object;
                        }
                    );
                return m.Object;

            }

            public bool IsCorrupted => false;
        }
        
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            return new MockRenderTarget();
        }

        public object TryGetFeature(Type featureType) => null;

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return Mock.Of<IRenderTargetBitmapImpl>();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<Geometry> children)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(
            PixelSize size,
            Vector dpi,
            PixelFormat format,
            AlphaFormat alphaFormat)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmap(
            PixelFormat format,
            AlphaFormat alphaFormat,
            IntPtr data,
            PixelSize size,
            Vector dpi,
            int stride)
        {
            throw new NotImplementedException();
        }

        public IGlyphRunImpl CreateGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, IReadOnlyList<GlyphInfo> glyphInfos)
        {
            return new MockGlyphRun(glyphInfos);
        }

        public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext graphicsContext) => this;

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            return Mock.Of<IGeometryImpl>();
        }

        public IGlyphRunBuffer AllocateGlyphRun(IGlyphTypeface glyphTypeface, float fontRenderingEmSize, int length)
        {
            return Mock.Of<IGlyphRunBuffer>();
        }

        public IHorizontalGlyphRunBuffer AllocateHorizontalGlyphRun(IGlyphTypeface glyphTypeface, float fontRenderingEmSize, int length)
        {
            return Mock.Of<IHorizontalGlyphRunBuffer>();
        }

        public IPositionedGlyphRunBuffer AllocatePositionedGlyphRun(IGlyphTypeface glyphTypeface, float fontRenderingEmSize, int length)
        {
            return Mock.Of<IPositionedGlyphRunBuffer>();
        }

        public bool SupportsIndividualRoundRects { get; set; }

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Rgba8888;
        public void Dispose()
        {
        }
    }
}
