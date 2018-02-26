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
        public IEllipseGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            throw new NotImplementedException();
        }

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

        public ILineGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            throw new NotImplementedException();
        }

        public IRectangleGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            throw new NotImplementedException();
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            return Mock.Of<IRenderTarget>();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(
            int width,
            int height,
            double dpiX,
            double dpiY)
        {
            return Mock.Of<IRenderTargetBitmapImpl>();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new MockStreamGeometryImpl();
        }

        public IWritableBitmapImpl CreateWritableBitmap(int width, int height, PixelFormat? format = default(PixelFormat?))
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

        public IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, int width, int height, int stride)
        {
            throw new NotImplementedException();
        }
    }
}
