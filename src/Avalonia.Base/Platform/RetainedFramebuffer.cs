using System;
using Avalonia.Platform.Internal;

namespace Avalonia.Platform;

internal class RetainedFramebuffer : IDisposable
{
    public PixelSize Size { get; }
    public  int RowBytes { get; }
    public PixelFormat Format { get; }
    public AlphaFormat AlphaFormat { get; }
    public IntPtr Address => _blob?.Address ?? throw new ObjectDisposedException(nameof(RetainedFramebuffer));
    private UnmanagedBlob? _blob;

    static PixelFormat ValidateKnownFormat(PixelFormat format) => format.BitsPerPixel % 8 == 0
        ? format
        : throw new ArgumentOutOfRangeException(nameof(format));

    public RetainedFramebuffer(PixelSize size, PixelFormat format, AlphaFormat alphaFormat)
        : this(size, ValidateKnownFormat(format), alphaFormat, format.BitsPerPixel / 8 * size.Width)
    {
        
    }
    
    public RetainedFramebuffer(PixelSize size, PixelFormat format, AlphaFormat alphaFormat, int rowBytes)
    {
        if (size.Width <= 0 || size.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        if (size.Width * (format.BitsPerPixel / 8) > rowBytes)
            throw new ArgumentOutOfRangeException(nameof(rowBytes));
        Size = size;
        RowBytes = rowBytes;
        Format = format;
        AlphaFormat = alphaFormat;
        _blob = new UnmanagedBlob(RowBytes * size.Height);
    }

    public ILockedFramebuffer Lock(Vector dpi, Action<RetainedFramebuffer> blit)
    {
        if (_blob == null)
            throw new ObjectDisposedException(nameof(RetainedFramebuffer));
        return  new LockedFramebuffer(_blob.Address, Size, RowBytes, dpi, Format, AlphaFormat, () =>
        {
            blit(this);
            GC.KeepAlive(this);
        });
    }

    public void Dispose()
    {
        _blob?.Dispose();
        _blob = null;
    }
}
