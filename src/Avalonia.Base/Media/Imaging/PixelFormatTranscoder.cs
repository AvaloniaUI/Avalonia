using System;
using Avalonia.Platform;
namespace Avalonia.Media.Imaging;

internal static unsafe class PixelFormatTranscoder
{
    public static void Transcode(
        IntPtr source,
        PixelSize srcSize,
        int sourceStride,
        PixelFormat srcFormat,
        AlphaFormat srcAlphaFormat,
        IntPtr dest,
        int destStride,
        PixelFormat destFormat,
        AlphaFormat destAlphaFormat)
    {
        var reader = GetReader(srcFormat);
        var writer = GetWriter(destFormat);

        var w = srcSize.Width;
        var h = srcSize.Height;

        for (var y = 0; y < h; y++)
        {
            reader.Reset(source + sourceStride * y);

            writer.Reset(dest + destStride * y);

            for (var x = 0; x < w; x++)
            {
                writer.WriteNext(GetConvertedPixel(reader.ReadNext(), srcAlphaFormat, destAlphaFormat));
            }
        }
    }

    private static Rgba8888Pixel GetConvertedPixel(Rgba8888Pixel pixel, AlphaFormat sourceAlpha, AlphaFormat destAlpha)
    {
        if (sourceAlpha != destAlpha)
        {
            if (sourceAlpha == AlphaFormat.Premul && destAlpha != AlphaFormat.Premul)
            {
                return ConvertFromPremultiplied(pixel);
            }

            if (sourceAlpha != AlphaFormat.Premul && destAlpha == AlphaFormat.Premul)
            {
                return ConvertToPremultiplied(pixel);
            }
        }

        return pixel;
    }

    private static Rgba8888Pixel ConvertToPremultiplied(Rgba8888Pixel pixel)
    {
        var factor = pixel.A / 255F;

        return new Rgba8888Pixel
        {
            R = (byte)(pixel.R * factor),
            G = (byte)(pixel.G * factor),
            B = (byte)(pixel.B * factor),
            A = pixel.A
        };
    }

    private static Rgba8888Pixel ConvertFromPremultiplied(Rgba8888Pixel pixel)
    {
        var factor = 1F / (pixel.A / 255F);

        return new Rgba8888Pixel
        {
            R = (byte)(pixel.R * factor),
            G = (byte)(pixel.G * factor),
            B = (byte)(pixel.B * factor),
            A = pixel.A
        };
    }

    private static IPixelFormatReader GetReader(PixelFormat format)
    {
        switch (format.FormatEnum)
        {
            case PixelFormatEnum.Rgb565:
                return new PixelFormatReader.Bgr565PixelFormatReader();
            case PixelFormatEnum.Rgba8888:
                return new PixelFormatReader.Rgba8888PixelFormatReader();
            case PixelFormatEnum.Bgra8888:
                return new PixelFormatReader.Bgra8888PixelFormatReader();
            case PixelFormatEnum.BlackWhite:
                return new PixelFormatReader.BlackWhitePixelFormatReader();
            case PixelFormatEnum.Gray2:
                return new PixelFormatReader.Gray2PixelFormatReader();
            case PixelFormatEnum.Gray4:
                return new PixelFormatReader.Gray4PixelFormatReader();
            case PixelFormatEnum.Gray8:
                return new PixelFormatReader.Gray8PixelFormatReader();
            case PixelFormatEnum.Gray16:
                return new PixelFormatReader.Gray16PixelFormatReader();
            case PixelFormatEnum.Gray32Float:
                return new PixelFormatReader.Gray32FloatPixelFormatReader();
            case PixelFormatEnum.Rgba64:
                return new PixelFormatReader.Rgba64PixelFormatReader();
            case PixelFormatEnum.Rgb24:
                return new PixelFormatReader.Rgb24PixelFormatReader();
            case PixelFormatEnum.Bgr24:
                return new PixelFormatReader.Bgr24PixelFormatReader();
            case PixelFormatEnum.Bgr555:
                return new PixelFormatReader.Bgr555PixelFormatReader();
            case PixelFormatEnum.Bgr565:
                return new PixelFormatReader.Bgr565PixelFormatReader();
            default:
                throw new NotSupportedException($"Pixel format {format} is not supported");
        }
    }

    private static IPixelFormatWriter GetWriter(PixelFormat format)
    {
        switch (format.FormatEnum)
        {
            case PixelFormatEnum.Rgb565:
                return new PixelFormatWriter.Bgr565PixelFormatWriter();
            case PixelFormatEnum.Rgba8888:
                return new PixelFormatWriter.Rgba8888PixelFormatWriter();
            case PixelFormatEnum.Bgra8888:
                return new PixelFormatWriter.Bgra8888PixelFormatWriter();
            case PixelFormatEnum.BlackWhite:
                return new PixelFormatWriter.BlackWhitePixelFormatWriter();
            case PixelFormatEnum.Gray2:
                return new PixelFormatWriter.Gray2PixelFormatWriter();
            case PixelFormatEnum.Gray4:
                return new PixelFormatWriter.Gray4PixelFormatWriter();
            case PixelFormatEnum.Gray8:
                return new PixelFormatWriter.Gray8PixelFormatWriter();
            case PixelFormatEnum.Gray16:
                return new PixelFormatWriter.Gray16PixelFormatWriter();
            case PixelFormatEnum.Gray32Float:
                return new PixelFormatWriter.Gray32FloatPixelFormatWriter();
            case PixelFormatEnum.Rgba64:
                return new PixelFormatWriter.Rgba64PixelFormatWriter();
            case PixelFormatEnum.Rgb24:
                return new PixelFormatWriter.Rgb24PixelFormatWriter();
            case PixelFormatEnum.Bgr24:
                return new PixelFormatWriter.Bgr24PixelFormatWriter();
            case PixelFormatEnum.Bgr555:
                return new PixelFormatWriter.Bgr555PixelFormatWriter();
            case PixelFormatEnum.Bgr565:
                return new PixelFormatWriter.Bgr565PixelFormatWriter();
            default:
                throw new NotSupportedException($"Pixel format {format} is not supported");
        }
    }
}
