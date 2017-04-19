// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Direct2D1.Media;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Direct2D1.Media.Imaging;

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

        private static readonly SharpDX.Direct2D1.Factory1 s_d2D1Factory =
#if DEBUG
            new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.MultiThreaded, SharpDX.Direct2D1.DebugLevel.Error);
#else
            new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.MultiThreaded, SharpDX.Direct2D1.DebugLevel.None);
#endif
        private static readonly SharpDX.DirectWrite.Factory s_dwfactory = new SharpDX.DirectWrite.Factory();

        private static readonly SharpDX.WIC.ImagingFactory s_imagingFactory = new SharpDX.WIC.ImagingFactory();

        private static readonly SharpDX.DXGI.Device s_dxgiDevice;

        private static readonly SharpDX.Direct2D1.Device s_d2D1Device;

        static Direct2D1Platform()
        {
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

            using (var d3dDevice = new SharpDX.Direct3D11.Device(
                SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport | SharpDX.Direct3D11.DeviceCreationFlags.VideoSupport,
                featureLevels))
            {
                s_dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>();
            }

            using (var factory1 = s_d2D1Factory.QueryInterface<SharpDX.Direct2D1.Factory1>())
            {
                s_d2D1Device = new SharpDX.Direct2D1.Device(factory1, s_dxgiDevice);
            }
        }

        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(s_instance)
                .Bind<SharpDX.Direct2D1.Factory>().ToConstant(s_d2D1Factory)
                .Bind<SharpDX.Direct2D1.Factory1>().ToConstant(s_d2D1Factory)
                .BindToSelf(s_dwfactory)
                .BindToSelf(s_imagingFactory)
                .BindToSelf(s_dxgiDevice)
                .BindToSelf(s_d2D1Device);
            SharpDX.Configuration.EnableReleaseOnFinalizer = true;
        }

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new WicBitmapImpl(s_imagingFactory, width, height);
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
            var nativeWindow = surfaces?.OfType<IPlatformHandle>().FirstOrDefault();
            if (nativeWindow != null)
            {
                if(nativeWindow.HandleDescriptor != "HWND")
                    throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from " + nativeWindow.HandleDescriptor);
                return new HwndRenderTarget(nativeWindow);
            }
            throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from any of provided surfaces");
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(
            int width,
            int height,
            double dpiX,
            double dpiY)
        {
            return new RenderTargetBitmapImpl(
                s_imagingFactory,
                s_d2D1Factory,
                s_dwfactory,
                width,
                height,
                dpiX,
                dpiY);
        }

        public IWritableBitmapImpl CreateWritableBitmap(int width, int height, PixelFormat? format = null)
        {
            return new WritableWicBitmapImpl(s_imagingFactory, width, height, format);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new WicBitmapImpl(s_imagingFactory, fileName);
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new WicBitmapImpl(s_imagingFactory, stream);
        }

        public IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, int width, int height, int stride)
        {
            return new WicBitmapImpl(s_imagingFactory, format, data, width, height, stride);
        }
    }
}
