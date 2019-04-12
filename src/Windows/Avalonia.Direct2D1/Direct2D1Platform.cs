// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;

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

        public IEnumerable<string> InstalledFontNames
        {
            get
            {
                var cache = Direct2D1FontCollectionCache.s_installedFontCollection;
                var length = cache.FontFamilyCount;
                for (int i = 0; i < length; i++)
                {
                    var names = cache.GetFontFamily(i).FamilyNames;
                    yield return names.GetString(0);
                }
            }
        }

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
            AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(s_instance);
            SharpDX.Configuration.EnableReleaseOnFinalizer = true;
        }

        public IBitmapImpl CreateBitmap(PixelSize size, Vector dpi)
        {
            return new WicBitmapImpl(size, dpi);
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new FormattedTextImpl(
                text,
                typeface,
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
    }
}
