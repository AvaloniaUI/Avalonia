using System;
using System.IO;

using SharpDX.WIC;

namespace Avalonia.Direct2D1.Media.Imaging
{
    internal enum WicImageFormat
    {
        Png,
        Jpeg,
        Bmp,
        Gif,
    }

    internal static class WicImageFormatExtensions
    {
        public static Guid ToContainerFormat(this WicImageFormat format)
        {
            return format switch
            {
                WicImageFormat.Png => ContainerFormatGuids.Png,
                WicImageFormat.Jpeg => ContainerFormatGuids.Jpeg,
                WicImageFormat.Bmp => ContainerFormatGuids.Bmp,
                WicImageFormat.Gif => ContainerFormatGuids.Gif,
                _ => throw new ArgumentException("Invalid image format."),
            };
        }

        public static BitmapEncoder CreateEncoder(this WicImageFormat format, ImagingFactory factory, Stream stream)
        {
            return format switch
            {
                WicImageFormat.Png => new PngBitmapEncoder(factory, stream),
                WicImageFormat.Jpeg => new JpegBitmapEncoder(factory, stream),
                WicImageFormat.Bmp => new BmpBitmapEncoder(factory, stream),
                WicImageFormat.Gif => new GifBitmapEncoder(factory, stream),
                _ => throw new ArgumentException("Invalid image format."),
            };
        }
    }
}
