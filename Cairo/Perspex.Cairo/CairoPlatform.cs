// -----------------------------------------------------------------------
// <copyright file="CairoPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo
{
    using System;
    using global::Cairo;
    using Perspex.Cairo.Media;
    using Perspex.Cairo.Media.Imaging;
    using Perspex.Platform;
    using Perspex.Threading;
    using Splat;

    public class CairoPlatform : IPlatformRenderInterface
    {
        private static CairoPlatform instance = new CairoPlatform();

        private static TextService textService = new TextService();

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => instance, typeof(IPlatformRenderInterface));
            locator.Register(() => textService, typeof(ITextService));
            //locator.Register(() => d2d1Factory, typeof(SharpDX.Direct2D1.Factory));
            //locator.Register(() => dwFactory, typeof(SharpDX.DirectWrite.Factory));
            //locator.Register(() => imagingFactory, typeof(SharpDX.WIC.ImagingFactory));
        }

        public ITextService TextService
        {
            get { /*return textService;*/ throw new NotImplementedException(); }
        }

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            throw new NotImplementedException();
            //return new BitmapImpl(imagingFactory, width, height);
        }

        public IRenderer CreateRenderer(IntPtr handle, double width, double height)
        {
            return new Renderer(handle, width, height);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            throw new NotImplementedException();
            //return new RenderTargetBitmapImpl(imagingFactory, d2d1Factory, width, height);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            throw new NotImplementedException();
            //return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            ImageSurface result = new ImageSurface(fileName);
            return new BitmapImpl(result);
        }
    }
}
