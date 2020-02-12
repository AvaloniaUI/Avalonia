using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;

namespace Avalonia.Benchmarks
{
    internal class NullRenderingPlatform : IPlatformRenderInterface
    {
        public IFormattedTextImpl CreateFormattedText(string text, Typeface typeface, double fontSize, TextAlignment textAlignment,
            TextWrapping wrapping, Size constraint, IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new NullFormattedTextImpl();
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            throw new NotImplementedException();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            throw new NotImplementedException();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            throw new NotImplementedException();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null)
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

        public IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            throw new NotImplementedException();
        }

        public IFontManagerImpl CreateFontManager()
        {
            return new MockFontManagerImpl();
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun, out double width)
        {
            throw new NotImplementedException();
        }
    }
}
