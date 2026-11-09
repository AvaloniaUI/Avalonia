using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Win32;
using Xunit;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;

namespace Avalonia.IntegrationTests.Win32;

public class OleDataObjectHelperTests
{
    private const int HeaderSize = 40;

    [Fact]
    public void Dib_Shorter_Than_Header_Is_Rejected()
    {
        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(new byte[HeaderSize - 1], out _, out _));
    }

    [Fact]
    public void Dib_Without_Pixel_Data_Is_Rejected()
    {
        var data = CreateDib(64, 64, 32, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 0);

        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out _, out _));
    }

    [Fact]
    public void Dib_With_Truncated_Pixel_Data_Is_Rejected()
    {
        // A 3x2 24 bpp row is 9 bytes, padded to 12.
        var data = CreateDib(3, 2, 24, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 12 + 9);

        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out _, out _));
    }

    [Fact]
    public void Dib_With_Header_Size_Past_End_Is_Rejected()
    {
        var data = CreateDib(1, 1, 32, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 4);
        MemoryMarshal.Write(data, (uint)(data.Length + 1));

        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out _, out _));
    }

    [Fact]
    public void Dib_Without_Color_Table_Is_Rejected()
    {
        var data = CreateDib(4, 1, 8, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 4);

        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out _, out _));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(0x10000, 0x10000)]
    public void Dib_With_Unsupported_Size_Is_Rejected(int width, int height)
    {
        var data = CreateDib(width, height, 32, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 16);

        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out _, out _));
    }

    [Theory]
    [InlineData((uint)BitmapCompressionMode.BI_RLE8, 8)]
    [InlineData((uint)BitmapCompressionMode.BI_JPEG, 0)]
    [InlineData((uint)BitmapCompressionMode.BI_PNG, 0)]
    [InlineData((uint)BitmapCompressionMode.BI_BITFIELDS, 24)]
    [InlineData((uint)BitmapCompressionMode.BI_RGB, 2)]
    public void Dib_With_Unsupported_Format_Is_Rejected(uint compression, int bitCount)
    {
        var data = CreateDib(1, 1, (ushort)bitCount, (BitmapCompressionMode)compression, extraSize: 1024, pixelDataSize: 1024);

        Assert.False(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out _, out _));
    }

    [Theory]
    [InlineData(32, (uint)BitmapCompressionMode.BI_RGB, 0, 3 * 4 * 2)]
    [InlineData(24, (uint)BitmapCompressionMode.BI_RGB, 0, 12 * 2)]
    [InlineData(32, (uint)BitmapCompressionMode.BI_BITFIELDS, 3 * 4, 3 * 4 * 2)]
    [InlineData(8, (uint)BitmapCompressionMode.BI_RGB, 256 * 4, 4 * 2)]
    [InlineData(1, (uint)BitmapCompressionMode.BI_RGB, 2 * 4, 4 * 2)]
    public void Valid_Dib_Is_Accepted(int bitCount, uint compression, int extraSize,
        int pixelDataSize)
    {
        var data = CreateDib(3, 2, (ushort)bitCount, (BitmapCompressionMode)compression, extraSize, pixelDataSize);

        Assert.True(OleDataObjectHelper.TryGetDibPixelDataOffset(data, out var header, out var offset));
        Assert.Equal(HeaderSize + extraSize, offset);
        Assert.Equal(3, header.biWidth);
        Assert.Equal(2, header.biHeight);
    }

    [Fact]
    public void Valid_Dib_Is_Read_As_Bitmap()
    {
        var data = CreateDib(3, 2, 32, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 3 * 4 * 2);

        using var bitmap = Assert.IsType<Bitmap>(ReadDib(data));
        Assert.Equal(new PixelSize(3, 2), bitmap.PixelSize);
    }

    [Fact]
    public void Truncated_Dib_Is_Not_Read()
    {
        var data = CreateDib(64, 64, 32, BitmapCompressionMode.BI_RGB, extraSize: 0, pixelDataSize: 0);

        Assert.Null(ReadDib(data));
    }

    private static byte[] CreateDib(int width, int height, ushort bitCount, BitmapCompressionMode compression,
        int extraSize, int pixelDataSize)
    {
        var header = new BITMAPINFOHEADER
        {
            biWidth = width,
            biHeight = height,
            biPlanes = 1,
            biBitCount = bitCount,
            biCompression = compression
        };
        header.Init();

        var data = new byte[HeaderSize + extraSize + pixelDataSize];
        MemoryMarshal.Write(data, in header);
        return data;
    }

    private static object? ReadDib(byte[] data)
    {
        var hGlobal = GlobalAlloc(GlobalAllocFlags.GHND, data.Length);
        Assert.NotEqual(IntPtr.Zero, hGlobal);

        try
        {
            var pointer = GlobalLock(hGlobal);
            Marshal.Copy(data, 0, pointer, data.Length);
            GlobalUnlock(hGlobal);

            var formatEtc = new FORMATETC
            {
                cfFormat = (ushort)ClipboardFormat.CF_DIB,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = TYMED.TYMED_HGLOBAL
            };

            return OleDataObjectHelper.ReadDataFromHGlobal(DataFormat.Bitmap, hGlobal, formatEtc);
        }
        finally
        {
            GlobalFree(hGlobal);
        }
    }
}
