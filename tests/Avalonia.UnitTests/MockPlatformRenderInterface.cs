using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using Moq;

namespace Avalonia.UnitTests
{
    public class MockPlatformRenderInterface : IPlatformRenderInterface
    {
        public IEnumerable<string> InstalledFontNames => new string[0];

        public IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return Mock.Of<IFormattedTextImpl>();
        }

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
            return Mock.Of<IGeometryImpl>();
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            return Mock.Of<IRenderTarget>();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return Mock.Of<IRenderTargetBitmapImpl>();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(
            PixelSize size,
            Vector dpi,
            PixelFormat? format = default(PixelFormat?))
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return Mock.Of<IBitmapImpl>();
        }

        public IBitmapImpl LoadBitmap(
            PixelFormat format,
            IntPtr data,
            PixelSize size,
            Vector dpi,
            int stride)
        {
            throw new NotImplementedException();
        }
    }
}
