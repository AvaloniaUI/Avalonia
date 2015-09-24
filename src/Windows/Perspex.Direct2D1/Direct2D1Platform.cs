// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using Perspex.Direct2D1.Media;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Direct2D1
{
    public class Direct2D1Platform : IPlatformRenderInterface
    {
        private static readonly Direct2D1Platform s_instance = new Direct2D1Platform();

        private static readonly SharpDX.Direct2D1.Factory s_d2D1Factory = new SharpDX.Direct2D1.Factory();

        private static readonly SharpDX.DirectWrite.Factory s_dwfactory = new SharpDX.DirectWrite.Factory();

        private static readonly SharpDX.WIC.ImagingFactory s_imagingFactory = new SharpDX.WIC.ImagingFactory();

        public static void Initialize() => PerspexLocator.CurrentMutable
            .Bind<IPlatformRenderInterface>().ToConstant(s_instance)
            .BindToSelf(s_d2D1Factory)
            .BindToSelf(s_dwfactory)
            .BindToSelf(s_imagingFactory);

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new BitmapImpl(s_imagingFactory, width, height);
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            return new FormattedTextImpl(text, fontFamily, fontSize, fontStyle, textAlignment, fontWeight);
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
            return new RenderTargetBitmapImpl(s_imagingFactory, s_d2D1Factory, width, height);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new BitmapImpl(s_imagingFactory, fileName);
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new BitmapImpl(s_imagingFactory, stream);
        }
    }
}
