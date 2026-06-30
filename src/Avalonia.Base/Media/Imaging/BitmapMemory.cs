using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging;

internal class BitmapMemory : IDisposable
{
    private readonly int _memorySize;
    private IntPtr _address;

    public BitmapMemory(PixelFormat format, AlphaFormat alphaFormat, PixelSize size)
    {
        Format = format;
        AlphaFormat = alphaFormat;
        Size = size;

        var bytesPerPixel = (format.BitsPerPixel + 7) / 8;

        RowBytes =  4 * ((size.Width * bytesPerPixel + 3) / 4);

        _memorySize = RowBytes * size.Height;
        _address = Marshal.AllocHGlobal(_memorySize);
        GC.AddMemoryPressure(_memorySize);
    }

    private void ReleaseUnmanagedResources()
    {
        var address = Interlocked.Exchange(ref _address, IntPtr.Zero);
        if (address != IntPtr.Zero)
        {
            GC.RemoveMemoryPressure(_memorySize);
            Marshal.FreeHGlobal(address);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~BitmapMemory()
    {
        ReleaseUnmanagedResources();
    }

    public IntPtr Address => _address;
    public PixelSize Size { get; }
    public int RowBytes { get; }
    public PixelFormat Format { get; }

    public AlphaFormat AlphaFormat { get; }

    public void CopyToRgba(AlphaFormat alphaFormat, IntPtr buffer, int stride)
    {
        PixelFormatTranscoder.Transcode(
            Address,
            Size,
            RowBytes,
            Format,
            AlphaFormat,
            buffer,
            stride,
            PixelFormat.Rgba8888,
            alphaFormat);
    }
}
