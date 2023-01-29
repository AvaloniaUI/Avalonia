using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Microsoft.Diagnostics.Runtime;

namespace Avalonia.Benchmarks
{
    internal class NullRenderingPlatform : IPlatformRenderInterface, IPlatformRenderInterfaceContext
    {
        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            return new MockStreamGeometryImpl();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            return new MockStreamGeometryImpl();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return new MockStreamGeometryImpl();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<Geometry> children)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
        {
            throw new NotImplementedException();
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            throw new NotImplementedException();
        }

        public bool IsLost => false;

        public object TryGetFeature(Type featureType) => null;

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            throw new NotImplementedException();
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

        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotImplementedException();
        }

        public IFontManagerImpl CreateFontManager()
        {
            return new MockFontManagerImpl();
        }

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            return new MockStreamGeometryImpl();
        }

        public IGlyphRunImpl CreateGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, 
            IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin)
        {
            return new MockGlyphRun(glyphInfos);
        }

        public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext graphicsContext)
        {
            return this;
        }

        public bool SupportsIndividualRoundRects => true;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Rgba8888;
        public bool IsSupportedBitmapPixelFormat(PixelFormat format) => true;

        public void Dispose()
        {
            
        }
    }
}
