// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Cairo.Media;
using Avalonia.Cairo.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Controls;

namespace Avalonia
{
    public static class GtkApplicationExtensions
    {
        public static AppBuilder UseCairo(this AppBuilder builder)
        {
            builder.RenderingSubsystem = Avalonia.Cairo.CairoPlatform.Initialize;
            return builder;
        }
    }
}

namespace Avalonia.Cairo
{
    using System.IO;
    using global::Cairo;

    public class CairoPlatform : IPlatformRenderInterface
    {
        private static readonly CairoPlatform s_instance = new CairoPlatform();

        private static readonly Pango.Context s_pangoContext = CreatePangoContext();

        public static void Initialize() => AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(s_instance);

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return new BitmapImpl(new Gdk.Pixbuf(Gdk.Colorspace.Rgb, true, 32, width, height));
        }

        public IFormattedTextImpl CreateFormattedText(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            Avalonia.Media.FontWeight fontWeight)
        {
            return new FormattedTextImpl(s_pangoContext, text, fontFamily, fontSize, fontStyle, textAlignment, fontWeight);
        }

        public IRenderTarget CreateRenderer(IPlatformHandle handle)
        {
            var window = handle as Gtk.Window;
            if (window == null)
                throw new NotSupportedException(string.Format(
                    "Don't know how to create a Cairo renderer from a '{0}' handle which isn't Gtk.Window",
                    handle.HandleDescriptor));

            window.DoubleBuffered = true;
            return new RenderTarget(window);
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
            var pixbuf = new Gdk.Pixbuf(fileName);

            return new BitmapImpl(pixbuf);
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            var pixbuf = new Gdk.Pixbuf(stream);

            return new BitmapImpl(pixbuf);
        }

        private static Pango.Context CreatePangoContext()
        {
            Gtk.Application.Init();
            return new Gtk.Invisible().CreatePangoContext();
        }
    }
}
