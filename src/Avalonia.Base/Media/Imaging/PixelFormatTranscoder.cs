using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Internal;
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
        var pixelCount = srcSize.Width * srcSize.Height;
        var bufferSize = pixelCount * Marshal.SizeOf<Rgba8888Pixel>();
        using var blob = new UnmanagedBlob(bufferSize);
      
        var pixels = new Span<Rgba8888Pixel>((void*)blob.Address, pixelCount);

        PixelFormatReader.Read(pixels, source, srcSize, sourceStride, srcFormat);

        PixelFormatWriter.Write(pixels, dest, srcSize, destStride, destFormat, destAlphaFormat, srcAlphaFormat);
    }
}
