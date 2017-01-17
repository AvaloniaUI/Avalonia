// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using Avalonia.Direct2D1.Media;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Rendering;

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
    public class Direct2D1Platform : IPlatformRenderInterface, IRendererFactory
    {
        private static readonly Direct2D1Platform s_instance = new Direct2D1Platform();

        private static readonly SharpDX.Direct2D1.Factory s_d2D1Factory =
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

        public static void Initialize() => AvaloniaLocator.CurrentMutable
            .Bind<IPlatformRenderInterface>().ToConstant(s_instance)
            .Bind<IRendererFactory>().ToConstant(s_instance)
            .BindToSelf(s_d2D1Factory)
            .BindToSelf(s_dwfactory)
            .BindToSelf(s_imagingFactory)
            .BindToSelf(s_dxgiDevice)
            .BindToSelf(s_d2D1Device);

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new WicBitmapImpl(s_imagingFactory, width, height);
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight,
            TextWrapping wrapping)
        {
            return new FormattedTextImpl(text, fontFamily, fontSize, fontStyle, textAlignment, fontWeight, wrapping);
        }

        public IRenderer CreateRenderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            return new Renderer(root, renderLoop);
        }

        public IRenderTarget CreateRenderTarget(IPlatformHandle handle)
        {
            if (handle.HandleDescriptor == "HWND")
            {
                return new HwndRenderTarget(handle.Handle);
            }
            else
            {
                throw new NotSupportedException(string.Format(
                    "Don't know how to create a Direct2D1 renderer from a '{0}' handle",
                    handle.HandleDescriptor));
            }
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            return new RenderTargetBitmapImpl(s_imagingFactory, s_d2D1Device.Factory, width, height);
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
    }
}
