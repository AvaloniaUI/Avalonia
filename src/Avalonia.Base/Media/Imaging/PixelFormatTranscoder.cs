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
        var pixels = PixelFormatReader.Read(source, srcSize, sourceStride, srcFormat);

        PixelFormatWriter.Write(dest, srcSize, destStride, destFormat, destAlphaFormat, srcAlphaFormat, pixels);
    }
}
