using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using SharpDX.DirectWrite;
using GlyphRun = Avalonia.Media.GlyphRun;
using TextAlignment = Avalonia.Media.TextAlignment;

namespace Avalonia
{
    public static class Direct2DApplicationExtensions
    {
        public static T UseDirect2D1<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseRenderingSubsystem(Direct2D1.Direct2D1Platform.Initialize, "Direct2D1");
            return builder;
        }
    }
}

namespace Avalonia.Direct2D1
{
    public class Direct2D1Platform : IPlatformRenderInterface
    {
        private static readonly Direct2D1Platform s_instance = new Direct2D1Platform();

        public static SharpDX.Direct3D11.Device Direct3D11Device { get; private set; }

        public static SharpDX.Direct2D1.Factory1 Direct2D1Factory { get; private set; }

        public static SharpDX.Direct2D1.Device Direct2D1Device { get; private set; }

        public static SharpDX.DirectWrite.Factory1 DirectWriteFactory { get; private set; }

        public static SharpDX.WIC.ImagingFactory ImagingFactory { get; private set; }

        public static SharpDX.DXGI.Device1 DxgiDevice { get; private set; }

        private static readonly object s_initLock = new object();
        private static bool s_initialized = false;

        internal static void InitializeDirect2D()
        {
            lock (s_initLock)
            {
                if (s_initialized)
                {
                    return;
                }
#if DEBUG
                try
                {
                    Direct2D1Factory = new SharpDX.Direct2D1.Factory1(
                        SharpDX.Direct2D1.FactoryType.MultiThreaded,
                            SharpDX.Direct2D1.DebugLevel.Error);
                }
                catch
                {
                    //
                }
#endif
                if (Direct2D1Factory == null)
                {
                    Direct2D1Factory = new SharpDX.Direct2D1.Factory1(
                        SharpDX.Direct2D1.FactoryType.MultiThreaded,
                        SharpDX.Direct2D1.DebugLevel.None);
                }

                using (var factory = new SharpDX.DirectWrite.Factory())
                {
                    DirectWriteFactory = factory.QueryInterface<SharpDX.DirectWrite.Factory1>();
                }

                ImagingFactory = new SharpDX.WIC.ImagingFactory();

                var featureLevels = new[]
                {
                    SharpDX.Direct3D.FeatureLevel.Level_11_1,
                    SharpDX.Direct3D.FeatureLevel.Level_11_0,
                    SharpDX.Direct3D.FeatureLevel.Level_10_1,
                    SharpDX.Direct3D.FeatureLevel.Level_10_0,
                    SharpDX.Direct3D.FeatureLevel.Level_9_3,
                    SharpDX.Direct3D.FeatureLevel.Level_9_2,
                    SharpDX.Direct3D.FeatureLevel.Level_9_1,
                };

                Direct3D11Device = new SharpDX.Direct3D11.Device(
                    SharpDX.Direct3D.DriverType.Hardware,
                    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport | SharpDX.Direct3D11.DeviceCreationFlags.VideoSupport,
                    featureLevels);

                DxgiDevice = Direct3D11Device.QueryInterface<SharpDX.DXGI.Device1>();

                Direct2D1Device = new SharpDX.Direct2D1.Device(Direct2D1Factory, DxgiDevice);

                s_initialized = true;
            }
        }

        public static void Initialize()
        {
            InitializeDirect2D();
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(s_instance)
                .Bind<IFontManagerImpl>().ToConstant(new FontManagerImpl())
                .Bind<ITextShaperImpl>().ToConstant(new TextShaperImpl());
            SharpDX.Configuration.EnableReleaseOnFinalizer = true;
        }

        public IBitmapImpl CreateBitmap(PixelSize size, Vector dpi)
        {
            return new WicBitmapImpl(size, dpi);
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new FormattedTextImpl(
                text,
                typeface,
                fontSize,
                textAlignment,
                wrapping,
                constraint,
                spans);
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var s in surfaces)
            {
                if (s is IPlatformHandle nativeWindow)
                {
                    if (nativeWindow.HandleDescriptor != "HWND")
                    {
                        throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from " +
                                                        nativeWindow.HandleDescriptor);
                    }

                    return new HwndRenderTarget(nativeWindow);
                }
                if (s is IExternalDirect2DRenderTargetSurface external)
                {
                    return new ExternalRenderTarget(external);
                }

                if (s is IFramebufferPlatformSurface fb)
                {
                    return new FramebufferShimRenderTarget(fb);
                }
            }
            throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from any of provided surfaces");
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return new WicRenderTargetBitmapImpl(size, dpi);
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null)
        {
            return new WriteableWicBitmapImpl(size, dpi, format);
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect) => new EllipseGeometryImpl(rect);
        public IGeometryImpl CreateLineGeometry(Point p1, Point p2) => new LineGeometryImpl(p1, p2);
        public IGeometryImpl CreateRectangleGeometry(Rect rect) => new RectangleGeometryImpl(rect);
        public IStreamGeometryImpl CreateStreamGeometry() => new StreamGeometryImpl();

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new WicBitmapImpl(fileName);
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new WicBitmapImpl(stream);
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new WicBitmapImpl(format, data, size, dpi, stride);
        }

        public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun, out double width)
        {
            var glyphTypeface = (GlyphTypefaceImpl)glyphRun.GlyphTypeface.PlatformImpl;

            var glyphCount = glyphRun.GlyphIndices.Length;

            var run = new SharpDX.DirectWrite.GlyphRun
            {
                FontFace = glyphTypeface.FontFace,
                FontSize = (float)glyphRun.FontRenderingEmSize
            };

            var indices = new short[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                indices[i] = (short)glyphRun.GlyphIndices[i];
            }

            run.Indices = indices;

            run.Advances = new float[glyphCount];

            width = 0;

            for (var i = 0; i < glyphCount; i++)
            {
                run.Advances[i] = (float)glyphRun.GlyphAdvances[i];
                width += run.Advances[i];
            }

            run.Offsets = new GlyphOffset[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                var offset = glyphRun.GlyphOffsets[i];

                run.Offsets[i] = new GlyphOffset
                {
                    AdvanceOffset = (float)offset.X,
                    AscenderOffset = (float)offset.Y
                };
            }

            return new GlyphRunImpl(run);
        }

        public bool SupportsIndividualRoundRects => false;
    }
}
