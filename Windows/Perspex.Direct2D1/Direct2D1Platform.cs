// -----------------------------------------------------------------------
// <copyright file="Direct2D1Platform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
    using Perspex.Direct2D1.Media;
    using Perspex.Platform;
    using Perspex.Threading;
    using Splat;

    public class Direct2D1Platform : IPlatformRenderInterface
    {
        private static Direct2D1Platform instance = new Direct2D1Platform();

        private static SharpDX.Direct2D1.Factory d2d1Factory = new SharpDX.Direct2D1.Factory();

        private static SharpDX.DirectWrite.Factory dwfactory = new SharpDX.DirectWrite.Factory();

        private static SharpDX.WIC.ImagingFactory imagingFactory = new SharpDX.WIC.ImagingFactory();

        private static TextService textService = new TextService(dwfactory);

        public ITextService TextService
        {
            get { return textService; }
        }

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => instance, typeof(IPlatformRenderInterface));
            locator.Register(() => textService, typeof(ITextService));
            locator.Register(() => d2d1Factory, typeof(SharpDX.Direct2D1.Factory));
            locator.Register(() => dwfactory, typeof(SharpDX.DirectWrite.Factory));
            locator.Register(() => imagingFactory, typeof(SharpDX.WIC.ImagingFactory));
        }

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new BitmapImpl(imagingFactory, width, height);
        }

        public IRenderer CreateRenderer(IPlatformHandle handle, double width, double height)
        {
            if (handle.HandleDescriptor == "HWND")
            {
                return new Renderer(handle.Handle, width, height);
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
            return new RenderTargetBitmapImpl(imagingFactory, d2d1Factory, width, height);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new BitmapImpl(imagingFactory, fileName);
        }
    }
}
