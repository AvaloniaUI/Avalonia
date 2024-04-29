using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging;

internal class BitmapMemory : IDisposable
{
    private readonly int _memorySize;

    public BitmapMemory(PixelFormat format, AlphaFormat alphaFormat, PixelSize size)
    {
        Format = format;
        AlphaFormat = alphaFormat;
        Size = size;
        RowBytes = (size.Width * format.BitsPerPixel + 7) / 8;
        _memorySize = RowBytes * size.Height;
        Address = Marshal.AllocHGlobal(_memorySize);
        GC.AddMemoryPressure(_memorySize);
    }

    private void ReleaseUnmanagedResources()
    {
        if (Address != IntPtr.Zero)
        {
            GC.RemoveMemoryPressure(_memorySize);
            Marshal.FreeHGlobal(Address);
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

    public IntPtr Address { get; private set; }
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
