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
            return FormattedTextImpl.Create(text, fontFamilyName, fontSize, fontStyle, textAlignment, fontWeight, wrapping);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        IBitmapImpl LoadBitmap(byte[] data)
        {
            var bitmap = new SKBitmap();
            if (!SKImageDecoder.DecodeMemory(data, bitmap))
            {
                throw new ArgumentException("Unable to load bitmap from provided data");
            }

            return new BitmapImpl(bitmap);
        }

        public IBitmapImpl LoadBitmap(System.IO.Stream stream)
        {
            using (var sr = new BinaryReader(stream))
            {
                return LoadBitmap(sr.ReadBytes((int)stream.Length));
            }
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return LoadBitmap(File.ReadAllBytes(fileName));
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
            return new WindowRenderTarget(handle.Handle);
        }
    }
}
