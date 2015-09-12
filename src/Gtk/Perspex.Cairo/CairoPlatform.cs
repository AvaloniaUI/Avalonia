// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Cairo.Media;
using Perspex.Cairo.Media.Imaging;
using Perspex.Media;
using Perspex.Platform;
using Splat;

namespace Perspex.Cairo
{
    using System.IO;
    using global::Cairo;

    public class CairoPlatform : IPlatformRenderInterface
    {
        private static readonly CairoPlatform s_instance = new CairoPlatform();

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => s_instance, typeof(IPlatformRenderInterface));
        }

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new BitmapImpl(new ImageSurface(Format.Argb32, width, height));
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            Perspex.Media.FontWeight fontWeight)
        {
            return new FormattedTextImpl(text, fontFamily, fontSize, fontStyle, textAlignment, fontWeight);
        }

        public IRenderer CreateRenderer(IPlatformHandle handle, double width, double height)
        {
            Locator.CurrentMutable.RegisterConstant(GetPangoContext(handle), typeof(Pango.Context));
            return new Renderer(handle, width, height);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            return new RenderTargetBitmapImpl(new ImageSurface(Format.Argb32, width, height));
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            ImageSurface result = new ImageSurface(fileName);
            return new BitmapImpl(result);
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        private Pango.Context GetPangoContext(IPlatformHandle handle)
        {
            switch (handle.HandleDescriptor)
            {
                case "GtkWindow":
                    var window = GLib.Object.GetObject(handle.Handle) as Gtk.Window;
                    return window.PangoContext;
                default:
                    throw new NotSupportedException(string.Format(
                        "Don't know how to get a Pango Context from a '{0}'.",
                        handle.HandleDescriptor));
            }
        }
    }
}
