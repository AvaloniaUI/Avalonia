using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Win32.Interop;

internal class Win32Icon : IDisposable
{
    public Win32Icon(Bitmap bitmap, PixelPoint hotSpot = default)
    {
        Handle = CreateIcon(bitmap, hotSpot);
    }
    
    public Win32Icon(IBitmapImpl bitmap, PixelPoint hotSpot = default)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream);
        memoryStream.Position = 0;
        using var bmp = new Bitmap(memoryStream);
        Handle = CreateIcon(bmp, hotSpot);
    }

    public Win32Icon(byte[] iconData, PixelSize size = default)
    {
        _bytes = iconData;

        (Handle, Size) = LoadIconFromData(iconData, ReplaceZeroesWithSystemMetrics(size));
        if (Handle == IntPtr.Zero)
        {
            using var bmp = new Bitmap(new MemoryStream(iconData));
            Handle = CreateIcon(bmp);
        }
    }

    public Win32Icon(Win32Icon original, PixelSize size = default)
    {
        _bytes = original._bytes ?? throw new ArgumentException("Original icon was created from a bitmap and cannot be copied.", nameof(original));
        
        (Handle, Size) = LoadIconFromData(_bytes, ReplaceZeroesWithSystemMetrics(size));
        if (Handle == IntPtr.Zero)
        {
            using var bmp = new Bitmap(new MemoryStream(_bytes));
            Handle = CreateIcon(bmp);
        }
    }

    public IntPtr Handle { get; private set; }
    public PixelSize Size { get; }
    
    private readonly byte[]? _bytes;
    
    IntPtr CreateIcon(Bitmap bitmap, PixelPoint hotSpot = default)
    {

        var mainBitmap = CreateHBitmap(bitmap);
        if (mainBitmap == IntPtr.Zero)
            throw new Win32Exception();
        var alphaBitmap = AlphaToMask(bitmap);

        try
        {
            if (alphaBitmap == IntPtr.Zero)
                throw new Win32Exception();
            var info = new UnmanagedMethods.ICONINFO
            {
                IsIcon = false,
                xHotspot = hotSpot.X,
                yHotspot = hotSpot.Y,
                MaskBitmap = alphaBitmap,
                ColorBitmap = mainBitmap
            };

            var hIcon = UnmanagedMethods.CreateIconIndirect(ref info);
            if (hIcon == IntPtr.Zero)
                throw new Win32Exception();
            return hIcon;
        }
        finally
        {
            UnmanagedMethods.DeleteObject(mainBitmap);
            UnmanagedMethods.DeleteObject(alphaBitmap);
        }
    }

    static IntPtr CreateHBitmap(Bitmap source)
    {
        using var fb = AllocFramebuffer(source.PixelSize, PixelFormats.Bgra8888);
        source.CopyPixels(fb, AlphaFormat.Unpremul);
        return UnmanagedMethods.CreateBitmap(source.PixelSize.Width, source.PixelSize.Height, 1, 32, fb.Address);
    }

    static unsafe IntPtr AlphaToMask(Bitmap source)
    {
        using var alphaMaskBuffer = AllocFramebuffer(source.PixelSize, PixelFormats.BlackWhite);
        var height = alphaMaskBuffer.Size.Height;
        var width = alphaMaskBuffer.Size.Width;

        if (!source.Format!.Value.HasAlpha)
        {
            Unsafe.InitBlock((void*)alphaMaskBuffer.Address, 0xff,
                (uint)(alphaMaskBuffer.RowBytes * alphaMaskBuffer.Size.Height));
        }
        else
        {
            using var argbBuffer = AllocFramebuffer(source.PixelSize, PixelFormat.Bgra8888);
            source.CopyPixels(argbBuffer, AlphaFormat.Unpremul);
            var pSource = (byte*)argbBuffer.Address;
            var pDest = (byte*)alphaMaskBuffer.Address;



            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    if (pSource[x * 4] == 0)
                    {
                        pDest[x / 8] |= (byte)(1 << (x % 8));
                    }
                }

                pSource += argbBuffer.RowBytes;
                pDest += alphaMaskBuffer.RowBytes;
            }

        }

        return UnmanagedMethods.CreateBitmap(width, height, 1, 1, alphaMaskBuffer.Address);
    }

    static LockedFramebuffer AllocFramebuffer(PixelSize size, PixelFormat format)
    {
        if (size.Width < 1 || size.Height < 1)
            throw new ArgumentOutOfRangeException();

        int stride = (size.Width * format.BitsPerPixel + 7) / 8;
        var data = Marshal.AllocHGlobal(size.Height * stride);
        if (data == IntPtr.Zero)
            throw new OutOfMemoryException();
        return new LockedFramebuffer(data, size, stride, new Vector(96, 96), format,
            () => Marshal.FreeHGlobal(data));
    }
    
    // Needs to be packed to 2 to get ICONDIRENTRY to follow immediately after idCount.
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct ICONDIR
    {
        // Must be 0
        public ushort idReserved;
        // Must be 1
        public ushort idType;
        // Count of entries
        public ushort idCount;
        // First entry (anysize array)
        public ICONDIRENTRY idEntries;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ICONDIRENTRY
    {
        // Width and height are 1 - 255 or 0 for 256
        public byte bWidth;
        public byte bHeight;
        public byte bColorCount;
        public byte bReserved;
        public ushort wPlanes;
        public ushort wBitCount;
        public uint dwBytesInRes;
        public uint dwImageOffset;
    }


    private static int s_bitDepth;

    private static PixelSize ReplaceZeroesWithSystemMetrics(PixelSize pixelSize) => new(
        pixelSize.Width == 0 ? UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXICON) : pixelSize.Width,
        pixelSize.Height == 0 ? UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYICON) : pixelSize.Height
        );

    private static unsafe (IntPtr, PixelSize) LoadIconFromData(byte[] iconData, PixelSize size)
    {
        if (iconData.Length < sizeof(ICONDIR))
            return default;

        if (s_bitDepth == 0)
        {
            IntPtr dc = UnmanagedMethods.GetDC(IntPtr.Zero);
            s_bitDepth = UnmanagedMethods.GetDeviceCaps(dc, UnmanagedMethods.DEVICECAP.BITSPIXEL);
            s_bitDepth *= UnmanagedMethods.GetDeviceCaps(dc, UnmanagedMethods.DEVICECAP.PLANES);
            UnmanagedMethods.ReleaseDC(IntPtr.Zero, dc);

            // If the bitdepth is 8, make it 4 because windows does not
            // choose a 256 color icon if the display is running in 256 color mode
            // due to palette flicker.
            if (s_bitDepth == 8)
            {
                s_bitDepth = 4;
            }
        }

        fixed (byte* b = iconData)
        {
            var dir = (ICONDIR*)b;

            if (dir->idReserved != 0 || dir->idType != 1 || dir->idCount == 0)
            {
                return default;
            }

            byte bestWidth = 0;
            byte bestHeight = 0;

            if (sizeof(ICONDIRENTRY) * (dir->idCount - 1) + sizeof(ICONDIR) > iconData.Length)
                return default;

            var entries = new ReadOnlySpan<ICONDIRENTRY>(&dir->idEntries, dir->idCount);
            var _bestBytesInRes = 0u;
            var _bestBitDepth = 0u;
            var _bestImageOffset = 0u;
            foreach (ICONDIRENTRY entry in entries)
            {
                bool fUpdateBestFit = false;
                uint iconBitDepth;
                if (entry.bColorCount != 0)
                {
                    iconBitDepth = 4;
                    if (entry.bColorCount < 0x10)
                    {
                        iconBitDepth = 1;
                    }
                }
                else
                {
                    iconBitDepth = entry.wBitCount;
                }

                // If it looks like if nothing is specified at this point then set the bits per pixel to 8.
                if (iconBitDepth == 0)
                {
                    iconBitDepth = 8;
                }

                //  Windows rules for specifing an icon:
                //
                //  1.  The icon with the closest size match.
                //  2.  For matching sizes, the image with the closest bit depth.
                //  3.  If there is no color depth match, the icon with the closest color depth that does not exceed the display.
                //  4.  If all icon color depth > display, lowest color depth is chosen.
                //  5.  color depth of > 8bpp are all equal.
                //  6.  Never choose an 8bpp icon on an 8bpp system.

                if (_bestBytesInRes == 0)
                {
                    fUpdateBestFit = true;
                }
                else
                {
                    int bestDelta = Math.Abs(bestWidth - size.Width) + Math.Abs(bestHeight - size.Height);
                    int thisDelta = Math.Abs(entry.bWidth - size.Width) + Math.Abs(entry.bHeight - size.Height);

                    if ((thisDelta < bestDelta) ||
                        (thisDelta == bestDelta && (iconBitDepth <= s_bitDepth && iconBitDepth > _bestBitDepth ||
                                                    _bestBitDepth > s_bitDepth && iconBitDepth < _bestBitDepth)))
                    {
                        fUpdateBestFit = true;
                    }
                }

                if (fUpdateBestFit)
                {
                    bestWidth = entry.bWidth;
                    bestHeight = entry.bHeight;
                    _bestImageOffset = entry.dwImageOffset;
                    _bestBytesInRes = entry.dwBytesInRes;
                    _bestBitDepth = iconBitDepth;
                }
            }

            if (_bestImageOffset > int.MaxValue || _bestBytesInRes > int.MaxValue)
            {
                return default;
            }

            uint endOffset;
            try
            {
                endOffset = checked(_bestImageOffset + _bestBytesInRes);
            }
            catch (OverflowException)
            {
                return default;
            }

            if (endOffset > iconData.Length)
            {
                return default;
            }

            var bestSize = new PixelSize(bestWidth, bestHeight);

            // Copy the bytes into an aligned buffer if needed.
            if ((_bestImageOffset % IntPtr.Size) != 0)
            {
                // Beginning of icon's content is misaligned.
                byte[] alignedBuffer = ArrayPool<byte>.Shared.Rent((int)_bestBytesInRes);
                Array.Copy(iconData, _bestImageOffset, alignedBuffer, 0, _bestBytesInRes);

                try
                {
                    fixed (byte* pbAlignedBuffer = alignedBuffer)
                    {
                        return (UnmanagedMethods.CreateIconFromResourceEx(pbAlignedBuffer, _bestBytesInRes, 1,
                            0x00030000, 0, 0, 0), bestSize);
                    }
                }
                finally
                {

                    ArrayPool<byte>.Shared.Return(alignedBuffer);
                }
            }
            else
            {
                try
                {
                    return (UnmanagedMethods.CreateIconFromResourceEx(checked(b + _bestImageOffset), _bestBytesInRes,
                        1, 0x00030000, 0, 0, 0), bestSize);
                }
                catch (OverflowException)
                {
                    return default;
                }
            }
        }
    }

    public void CopyTo(Stream stream)
    {
        stream.Write(_bytes ?? throw new InvalidOperationException("Icon was created from a bitmap, not Win32 icon data"), 0, _bytes.Length);
    }

    public void Dispose()
    {
        UnmanagedMethods.DestroyIcon(Handle);
        Handle = IntPtr.Zero;
    }
}
