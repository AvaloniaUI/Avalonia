// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Cairo.Media;
using Avalonia.Cairo.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Controls;

namespace Avalonia
{
    public static class GtkApplicationExtensions
    {
        public static T UseCairo<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseRenderingSubsystem(Cairo.CairoPlatform.Initialize, "Cairo");
            return builder;
        }
    }
}

namespace Avalonia.Cairo
{
    using System.IO;
    using global::Cairo;
    using Rendering;

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
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new FormattedTextImpl(
                s_pangoContext,
                text,
                typeface,
                textAlignment,
                wrapping,
                constraint,
                spans);
        }

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            var accessor = surfaces?.OfType<Func<Gdk.Drawable>>().FirstOrDefault();
            if(accessor!=null)
                return new RenderTarget(accessor);

            throw new NotSupportedException(string.Format(
                "Don't know how to create a Cairo renderer from any of the provided surfaces."));
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height, double dpiX, double dpiY)
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

        public IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, int width, int height, int stride)
        {
            throw new NotSupportedException("No proper control over pixel format with Cairo, use Skia backend instead");
        }

        public IWritableBitmapImpl CreateWritableBitmap(int width, int height, PixelFormat? fmt)
        {
            throw new NotSupportedException("No proper support with Cairo, use Skia backend instead");
        }
    }
}
