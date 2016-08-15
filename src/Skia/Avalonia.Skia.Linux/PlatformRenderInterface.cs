using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;

using SkiaSharp;

namespace Avalonia.Skia
{
    public class PlatformRenderInterface : IPlatformRenderInterface
    {
        public IBitmapImpl CreateBitmap(int width, int height)
        {
            return CreateRenderTargetBitmap(width, height);
        }

        public IFormattedTextImpl CreateFormattedText(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight, TextWrapping wrapping)
        {
            return new FormattedTextImpl(text, fontFamilyName, fontSize, fontStyle, textAlignment, fontWeight, wrapping);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(System.IO.Stream stream)
        {
            using (var s = new SKManagedStream(stream))
            {
                using (var codec = SKCodec.Create(s))
                {
                    var info = codec.Info;
                    var bitmap = new SKBitmap(info.Width, info.Height, SKImageInfo.PlatformColorType, info.IsOpaque ? SKAlphaType.Opaque : SKAlphaType.Premul);

                    IntPtr length;
                    var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels(out length));
                    if (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput)
                    {
                        return new BitmapImpl(bitmap);
                    }
                    else
                    {
                        throw new ArgumentException("Unable to load bitmap from provided data");
                    }
                }
            }
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return LoadBitmap(stream);
            }
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            if (width < 1)
                throw new ArgumentException("Width can't be less than 1", nameof(width));
            if (height < 1)
                throw new ArgumentException("Height can't be less than 1", nameof(height));

            return new BitmapImpl(width, height);
        }

        public IRenderTarget CreateRenderer(IPlatformHandle handle)
        {
			var window = handle as Gtk.Window;
			if (window == null)
				throw new NotSupportedException(string.Format(
					"Don't know how to create a Cairo renderer from a '{0}' handle which isn't Gtk.Window",
					handle.HandleDescriptor));

			window.DoubleBuffered = true;
			return new WindowRenderTarget (window);
            //return new WindowRenderTarget(handle.Handle);
        }
    }
}
