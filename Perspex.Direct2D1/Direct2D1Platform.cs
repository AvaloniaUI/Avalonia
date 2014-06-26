// -----------------------------------------------------------------------
// <copyright file="Direct2D1Initialize.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
    using Perspex.Direct2D1.Media;
    using Perspex.Platform;
    using Splat;

    public class Direct2D1Platform : IPlatformInterface
    {
        private static Direct2D1Platform instance = new Direct2D1Platform();

        private static SharpDX.Direct2D1.Factory d2d1Factory = new SharpDX.Direct2D1.Factory();

        private static SharpDX.DirectWrite.Factory dwFactory = new SharpDX.DirectWrite.Factory();

        private static SharpDX.WIC.ImagingFactory imagingFactory = new SharpDX.WIC.ImagingFactory();

        private static TextService textService = new TextService(dwFactory);

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => instance, typeof(IPlatformInterface));
            locator.Register(() => d2d1Factory, typeof(SharpDX.Direct2D1.Factory));
            locator.Register(() => dwFactory, typeof(SharpDX.DirectWrite.Factory));
            locator.Register(() => imagingFactory, typeof(SharpDX.WIC.ImagingFactory));
        }

        public ITextService TextService
        {
            get { return textService; }
        }

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new BitmapImpl(imagingFactory, width, height);
        }

        public IRenderer CreateRenderer(IntPtr handle, double width, double height)
        {
            return new Renderer(handle, width, height);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            return new RenderTargetBitmapImpl(imagingFactory, d2d1Factory, width, height);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }
    }
}
