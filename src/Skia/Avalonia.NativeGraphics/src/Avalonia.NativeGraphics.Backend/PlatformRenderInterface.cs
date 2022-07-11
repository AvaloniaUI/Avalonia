using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;

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
        public IAvgFactory Factory => _avgFactory;

        public IGeometryImpl CreateEllipseGeometry(Rect rect)
        {
            return new EllipseGeometry(_avgFactory, rect);
        }

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
        {
            return new LineGeometry(_avgFactory, p1, p2);
        }

        public IGeometryImpl CreateRectangleGeometry(Rect rect)
        {
            return new RectangleGeometry(_avgFactory, rect);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl(_avgFactory);
        }

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<Geometry> children)
        {
            Console.WriteLine("Create geomGroup");
            return new GeometryImpl(_avgFactory);
        }

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
        {
            Console.WriteLine("Create combinedGeom");
            return new GeometryImpl(_avgFactory);
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
            return new ImmutableBitmap(stream);
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

        /// <inheritdoc />
        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun)
        {
            FontManagerImpl fontManagerImpl = (FontManagerImpl)AvaloniaLocator.Current.GetService<IFontManagerImpl>();

            var count = glyphRun.GlyphIndices.Count;
            var glyphTypeface = (GlyphTypefaceImpl)glyphRun.GlyphTypeface.PlatformImpl;
            var typeface = glyphTypeface.Typeface;

            var avgGlyphRun = _avgFactory.CreateAvgGlyphRun(fontManagerImpl.Native, typeface);
            var scale = (float)(glyphRun.FontRenderingEmSize / glyphTypeface.DesignEmHeight);
            var glyphRunImpl = new GlyphRunImpl(avgGlyphRun);
            glyphRunImpl.SetFontSize((float)glyphRun.FontRenderingEmSize);

            if (glyphRun.GlyphOffsets == null)
            {
                if (glyphTypeface.IsFixedPitch)
                {
                    glyphRunImpl.AllocRun(glyphRun.GlyphIndices.Count);

                    var glyphs = glyphRunImpl.GetGlyphSpan();

                    for (int i = 0; i < glyphs.Length; i++)
                    {
                        glyphs[i] = glyphRun.GlyphIndices[i];
                    }

                    glyphRunImpl.BuildText();
                }
                else
                {
                    glyphRunImpl.AllocHorizontalRun(count);

                    var positions = glyphRunImpl.GetPositionsSpan();

                    var width = 0d;

                    for (var i = 0; i < count; i++)
                    {
                        positions[i] = (float)width;

                        if (glyphRun.GlyphAdvances == null)
                        {
                            width += glyphTypeface.GetGlyphAdvance(glyphRun.GlyphIndices[i]) * scale;
                        }
                        else
                        {
                            width += glyphRun.GlyphAdvances[i];
                        }
                    }

                    var glyphs = glyphRunImpl.GetGlyphSpan();

                    for (int i = 0; i < glyphs.Length; i++)
                    {
                        glyphs[i] = glyphRun.GlyphIndices[i];
                    }

                    glyphRunImpl.BuildText();
                }
            }
            else
            {
                glyphRunImpl.AllocPositionedRun(count);

                var glyphPositions = glyphRunImpl.GetPositionsVectorSpan();

                var currentX = 0.0;

                for (var i = 0; i < count; i++)
                {
                    var glyphOffset = glyphRun.GlyphOffsets[i];

                    glyphPositions[i] = new AvgSkPosition()
                    {
                        x = (float)(currentX + glyphOffset.X),
                        y = (float)glyphOffset.Y
                    };

                    if (glyphRun.GlyphAdvances == null)
                    {
                        currentX += glyphTypeface.GetGlyphAdvance(glyphRun.GlyphIndices[i]) * scale;
                    }
                    else
                    {
                        currentX += glyphRun.GlyphAdvances[i];
                    }
                }

                var glyphs = glyphRunImpl.GetGlyphSpan();

                for (int i = 0; i < glyphs.Length; i++)
                {
                    glyphs[i] = glyphRun.GlyphIndices[i];
                }

                glyphRunImpl.BuildText();
            }

            return glyphRunImpl;
        }

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            throw new NotImplementedException();
        }

        public bool SupportsIndividualRoundRects => true;
        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;
        public PixelFormat DefaultPixelFormat => PixelFormat.Bgra8888;
    }
}
