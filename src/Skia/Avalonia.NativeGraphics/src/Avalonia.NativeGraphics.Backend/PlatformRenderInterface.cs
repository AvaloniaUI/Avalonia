using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.NativeGraphics.Backend
{
    internal class PlatformRenderInterface : IPlatformRenderInterface
    {
        private readonly IAvgFactory _avgFactory;
        private readonly IPlatformOpenGlInterface _gl;
        
        class GetProcAddressShim : CallbackBase, IAvgGetProcAddressDelegate
        {
            private readonly Func<string, IntPtr> _cb;

            public GetProcAddressShim(Func<string, IntPtr> cb)
            {
                _cb = cb;
            }
            
            public IntPtr GetProcAddress(string proc) => _cb(proc);
        }
        
        public PlatformRenderInterface(IAvgFactory avgFactory)
        {
            _avgFactory = avgFactory;
            _gl = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();

            var gl = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>()!;
            using (gl.PrimaryContext.MakeCurrent())
            {
                var wrapper = new GlGpuControl(gl.PrimaryContext);
                Gpu = avgFactory.CreateGlGpu(gl.PrimaryContext.Version.Type == GlProfileType.OpenGLES ? 1 : 0, wrapper);
            }
        }

        public IAvgGpu Gpu { get; }

        public IFormattedTextImpl CreateFormattedText(string text, Typeface typeface, double fontSize, TextAlignment textAlignment,
            TextWrapping wrapping, Size constraint, IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new FormattedTextStub(text);
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            return new GeometryImpl();
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            return new GeometryImpl();
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return new GeometryImpl();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<Geometry> children)
        {
            return new GeometryImpl();
        }

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
        {
            return new GeometryImpl();
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            var glSurface = surfaces.OfType<IGlPlatformSurface>()
                .First();

            return new AvgRenderTarget(_avgFactory.CreateGlGpuRenderTarget(Gpu,
                new GlRenderTargetWrapper(glSurface.CreateGlRenderTarget())));
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            throw new NotSupportedException();
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            throw new NotSupportedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new BitmapStub(new MemoryStream(File.ReadAllBytes(fileName)));
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new BitmapStub(stream);
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotSupportedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            throw new NotSupportedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
        {
            throw new NotSupportedException();
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
        {
            throw new NotSupportedException();
        }

        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new BitmapStub(stream);
        }

        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new BitmapStub(stream);
        }

        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new BitmapStub(new MemoryStream());
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi,
            int stride)
        {
            return new BitmapStub(new MemoryStream());
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun, out double width)
        {
            width = 10;
            return new GlyphRunStub();
        }

        public bool SupportsIndividualRoundRects => false;
        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;
        public PixelFormat DefaultPixelFormat => PixelFormat.Bgra8888;
    }
}