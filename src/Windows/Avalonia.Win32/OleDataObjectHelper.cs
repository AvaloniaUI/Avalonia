using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Utilities;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using STGMEDIUM = Avalonia.Win32.Interop.UnmanagedMethods.STGMEDIUM;

namespace Avalonia.Win32;

/// <summary>
/// Contains helper methods to read from and write to an HGlobal, for <see cref="Win32Com.IDataObject"/> interop.
/// </summary>
internal static class OleDataObjectHelper
{
    private const int SRCCOPY = 0x00CC0020;

    public static FORMATETC ToFormatEtc(ushort formatId)
        => new()
        {
            cfFormat = formatId,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            ptd = IntPtr.Zero,
            lindex = -1,
            tymed = formatId == (ushort)ClipboardFormat.CF_BITMAP ? TYMED.TYMED_GDI : TYMED.TYMED_HGLOBAL
        };

    public static unsafe object? TryGet(this Win32Com.IDataObject oleDataObject, DataFormat format)
    {
        if (TryGetContainedFormat(oleDataObject, format) is not { } formatId)
            return null;

        var medium = new STGMEDIUM();
        var formatEtc = ToFormatEtc(formatId);
        var result = oleDataObject.GetData(&formatEtc, &medium);
        if (result != (uint)HRESULT.S_OK)
            return null;

        try
        {
            if (medium.unionmember != IntPtr.Zero)
            {
                if (medium.tymed == TYMED.TYMED_HGLOBAL)
                {
                    var hGlobal = medium.unionmember;
                    return ReadDataFromHGlobal(format, hGlobal, formatEtc);
                }
                else if (medium.tymed == TYMED.TYMED_GDI)
                {
                    var bitmapHandle = medium.unionmember;
                    return ReadDataFromGdi(bitmapHandle);
                }
            }
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        return null;
    }

    private static ushort? TryGetContainedFormat(Win32Com.IDataObject oleDataObject, DataFormat format)
    {
        // Bitmap is not a real format, find the first matching platform format, if any.
        if (DataFormat.Bitmap.Equals(format))
        {
            foreach (var imageFormat in ClipboardFormatRegistry.ImageFormats)
            {
                if (TryGetContainedFormatCore(oleDataObject, imageFormat) is { } formatId)
                    return formatId;
            }

            return null;
        }

        return TryGetContainedFormatCore(oleDataObject, format);

        static unsafe ushort? TryGetContainedFormatCore(Win32Com.IDataObject oleDataObject, DataFormat format)
        {
            Debug.Assert(format != DataFormat.Bitmap);

            var formatId = ClipboardFormatRegistry.GetOrAddFormat(format);
            var formatEtc = ToFormatEtc(formatId);
            return oleDataObject.QueryGetData(&formatEtc) == (uint)HRESULT.S_OK ? formatId : null;
        }
    }

    private static unsafe object? ReadDataFromGdi(nint bitmapHandle)
    {
        var bitmap = new BITMAP();
        unsafe
        {
            var pBitmap = &bitmap;
            if ((uint)GetObject(bitmapHandle, Marshal.SizeOf(bitmap), (IntPtr)pBitmap) == 0)
                return null;

            var bitmapInfoHeader = new BITMAPINFOHEADER()
            {
                biWidth = bitmap.bmWidth,
                biHeight = bitmap.bmHeight,
                biPlanes = bitmap.bmPlanes,
                biBitCount = 32,
                biCompression = 0,
                biSizeImage = (uint)(bitmap.bmWidth * 4 * Math.Abs(bitmap.bmHeight))
            };

            bitmapInfoHeader.Init();

            using var screenDc = GdiDC.Get();
            if (screenDc.IsInvalid)
                return null;

            using var destDc = GdiDC.CreateCompatible(screenDc);
            if (destDc.IsInvalid)
                return null;

            using var sourceDc = GdiDC.CreateCompatible(screenDc);
            if (sourceDc.IsInvalid)
                return null;

            using var section = new GdiObject(
                CreateDIBSection(destDc, ref bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0));
            if (section.IsInvalid)
                return null;

            using var selectedSection = destDc.Select(section);
            // The source bitmap belongs to the STGMEDIUM, so it is only selected here, never deleted.
            using var selectedSource = sourceDc.Select(bitmapHandle);

            if (StretchBlt(destDc, 0, bitmapInfoHeader.biHeight, bitmapInfoHeader.biWidth, -bitmapInfoHeader.biHeight, sourceDc, 0, 0, bitmap.bmWidth, bitmap.bmHeight, SRCCOPY) == 0)
                return null;

            return new Bitmap(Platform.PixelFormats.Bgra8888,
                Platform.AlphaFormat.Opaque,
                lbBits,
                new PixelSize(bitmapInfoHeader.biWidth, bitmapInfoHeader.biHeight),
                new Vector(96, 96),
                bitmapInfoHeader.biWidth * 4);
        }
    }

    public unsafe static object? ReadDataFromHGlobal(DataFormat format, IntPtr hGlobal, FORMATETC formatEtc)
    {
        if (DataFormat.Text.Equals(format))
            return ReadStringFromHGlobal(hGlobal);

        if (DataFormat.File.Equals(format))
        {
            return ReadFileNamesFromHGlobal(hGlobal)
                .Select(fileName => StorageProviderHelpers.TryCreateBclStorageItem(fileName) as IStorageItem)
                .Where(f => f is not null)
                .ToArray();
        }

        if (DataFormat.Bitmap.Equals(format))
        {
            if (formatEtc.cfFormat == (ushort)ClipboardFormat.CF_DIB)
            {
                var data = ReadBytesFromHGlobal(hGlobal);
                if (!TryGetDibPixelDataOffset(data, out var sourceHeader, out var pixelDataOffset))
                    return null;

                fixed (byte* ptr = data)
                {
                    var destHeader = new BITMAPINFOHEADER()
                    {
                        biWidth = sourceHeader.biWidth,
                        biHeight = sourceHeader.biHeight,
                        biPlanes = sourceHeader.biPlanes,
                        biBitCount = 32,
                        biCompression = BitmapCompressionMode.BI_RGB,
                        biSizeImage = (uint)(sourceHeader.biWidth * 4 * Math.Abs(sourceHeader.biHeight))
                    };

                    destHeader.Init();

                    using var screenDc = GdiDC.Get();
                    if (screenDc.IsInvalid)
                        return null;

                    using var destDc = GdiDC.CreateCompatible(screenDc);
                    if (destDc.IsInvalid)
                        return null;

                    using var section = new GdiObject(
                        CreateDIBSection(destDc, ref destHeader, 0, out var lbBits, IntPtr.Zero, 0));
                    if (section.IsInvalid)
                        return null;

                    using var selectedSection = destDc.Select(section);

                    if (StretchDIBits(destDc,
                            0,
                            sourceHeader.biHeight - 1,
                            sourceHeader.biWidth,
                            -sourceHeader.biHeight,
                            0,
                            0,
                            destHeader.biWidth,
                            destHeader.biHeight,
                            (IntPtr)(ptr + pixelDataOffset),
                            (IntPtr)ptr,
                            0,
                            SRCCOPY
                            ) == 0)
                        return null;

                    return new Bitmap(Platform.PixelFormats.Bgra8888,
                        Platform.AlphaFormat.Opaque,
                        lbBits,
                        new PixelSize(destHeader.biWidth, destHeader.biHeight),
                        new Vector(96, 96),
                        destHeader.biWidth * 4);
                }
            }
            else
            {
                var data = ReadBytesFromHGlobal(hGlobal);
                var stream = new MemoryStream(data);
                return new Bitmap(stream);
            }
        }

        if (format is DataFormat<string>)
            return ReadStringFromHGlobal(hGlobal);

        if (format is DataFormat<byte[]>)
            return ReadBytesFromHGlobal(hGlobal);

        return null;
    }

    /// <summary>
    /// Checks that a packed CF_DIB has a supported format and that its header, color table and pixels
    /// fit in <paramref name="data"/>.
    /// </summary>
    internal static unsafe bool TryGetDibPixelDataOffset(ReadOnlySpan<byte> data, out BITMAPINFOHEADER header,
        out int pixelDataOffset)
    {
        header = default;
        pixelDataOffset = 0;

        if (data.Length < sizeof(BITMAPINFOHEADER))
            return false;

        header = MemoryMarshal.Read<BITMAPINFOHEADER>(data);

        if (header.biSize < sizeof(BITMAPINFOHEADER) || header.biSize > data.Length)
            return false;

        if (header.biWidth <= 0 || header.biHeight <= 0 || header.biPlanes != 1)
            return false;

        var isSupported = header.biCompression switch
        {
            BitmapCompressionMode.BI_RGB => header.biBitCount is 1 or 4 or 8 or 16 or 24 or 32,
            BitmapCompressionMode.BI_BITFIELDS => header.biBitCount is 16 or 32,
            _ => false
        };
        if (!isSupported)
            return false;

        if (header.biBitCount <= 8 && header.biClrUsed > 1u << header.biBitCount)
            return false;

        // The 32 bpp copy that we create must fit in an int.
        if ((long)header.biWidth * 4 * header.biHeight > int.MaxValue)
            return false;

        var stride = ((long)header.biWidth * header.biBitCount + 31) / 32 * 4;
        var offset = (long)header.biSize + GetExtraHeaderSize(header);
        if (offset + stride * header.biHeight > data.Length)
            return false;

        pixelDataOffset = (int)offset;
        return true;
    }

    private static int GetExtraHeaderSize(in BITMAPINFOHEADER header)
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader
        switch (header.biCompression)
        {
            // If biCompression equals BI_RGB and the bitmap uses 8 bpp or less, the bitmap has a color table immediately
            // following the BITMAPINFOHEADER structure. The color table consists of an array of RGBQUAD values. The size
            // of the array is given by the biClrUsed member.
            // If biClrUsed is zero, the array contains the maximum number of colors for the given bitdepth; that is,
            // 2^biBitCount colors.
            case BitmapCompressionMode.BI_RGB when header.biBitCount <= 8:
                return (header.biClrUsed == 0 ? 1 << header.biBitCount : (int)header.biClrUsed) * 4;

            // If biCompression equals BI_BITFIELDS, the bitmap uses three DWORD color masks (red, green, and blue,
            // respectively), which specify the byte layout of the pixels. The 1 bits in each mask indicate the bits for
            // that color within the pixel.
            case BitmapCompressionMode.BI_BITFIELDS:
                return 3 * 4;

            default:
                return 0;
        }
    }

    private static string? ReadStringFromHGlobal(IntPtr hGlobal)
    {
        var sourcePtr = GlobalLock(hGlobal);
        try
        {
            return Marshal.PtrToStringAuto(sourcePtr);
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    private static List<string> ReadFileNamesFromHGlobal(IntPtr hGlobal)
    {
        var fileCount = DragQueryFile(hGlobal, -1, null, 0);
        var files = new List<string>(fileCount);

        for (var i = 0; i < fileCount; i++)
        {
            var pathLength = DragQueryFile(hGlobal, i, null, 0);
            var sb = StringBuilderCache.Acquire(pathLength + 1);

            if (DragQueryFile(hGlobal, i, sb, sb.Capacity) == pathLength)
                files.Add(StringBuilderCache.GetStringAndRelease(sb));
            else
                StringBuilderCache.Release(sb);
        }

        return files;
    }

    private static byte[] ReadBytesFromHGlobal(IntPtr hGlobal)
    {
        var source = GlobalLock(hGlobal);
        try
        {
            var size = (int)GlobalSize(hGlobal);
            var data = new byte[size];
            Marshal.Copy(source, data, 0, size);
            return data;
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    public unsafe static uint WriteDataToHGlobal(IDataTransfer dataTransfer, DataFormat format, ref IntPtr hGlobal)
    {
        if (DataFormat.Text.Equals(format))
        {
            var text = dataTransfer.TryGetValue(DataFormat.Text);
            return WriteStringToHGlobal(ref hGlobal, text ?? string.Empty);
        }

        if (DataFormat.File.Equals(format))
        {
            var files = dataTransfer.TryGetValues(DataFormat.File) ?? [];

            IEnumerable<string> fileNames = files
                .Select(StorageProviderExtensions.TryGetLocalPath)
                .Where(path => path is not null)!;

            return WriteFileNamesToHGlobal(ref hGlobal, fileNames);
        }

        if (ClipboardFormatRegistry.DibDataFormat.Equals(format) 
            || ClipboardFormatRegistry.DibV5DataFormat.Equals(format))
        {
            var bitmap = dataTransfer.TryGetValue(DataFormat.Bitmap);
            if (bitmap != null)
            {
                bool isV5 = ClipboardFormatRegistry.DibV5DataFormat.Equals(format);
                var pixelSize = bitmap.PixelSize;
                var bpp = bitmap.Format?.BitsPerPixel ?? 0;
                var stride = ((bitmap.Format?.BitsPerPixel ?? 0) / 8) * pixelSize.Width;
                var buffer = new byte[stride * pixelSize.Height];
                fixed (byte* bytes = buffer)
                {
                    bitmap.CopyPixels(new PixelRect(pixelSize), (IntPtr)bytes, buffer.Length, stride);

                    if (!isV5)
                    {
                        var infoHeader = new BITMAPINFOHEADER()
                        {
                            biSizeImage = (uint)buffer.Length,
                            biWidth = pixelSize.Width,
                            biHeight = -pixelSize.Height,
                            biBitCount = (ushort)bpp,
                            biPlanes = 1,
                            biCompression = BitmapCompressionMode.BI_RGB,
                        };
                        infoHeader.Init();

                        var imageData = new byte[infoHeader.biSize + infoHeader.biSizeImage];

                        fixed (byte* image = imageData)
                        {
                            Marshal.StructureToPtr(infoHeader, (IntPtr)image, false);
                            new Span<byte>(bytes, buffer.Length).CopyTo(new Span<byte>((image + infoHeader.biSize), buffer.Length));

                            return WriteBytesToHGlobal(ref hGlobal, imageData);
                        }
                    }
                    else
                    {
                        var infoHeader = new BITMAPV5HEADER()
                        {
                            bV5Width = pixelSize.Width,
                            bV5Height = -pixelSize.Height,
                            bV5Planes = 1,
                            bV5BitCount = (ushort)bpp,
                            bV5Compression = bpp > 16 ? BitmapCompressionMode.BI_BITFIELDS : BitmapCompressionMode.BI_RGB,
                            bV5SizeImage = (uint)buffer.Length,
                            bV5RedMask = GetRedMask(bitmap),
                            bV5BlueMask = GetBlueMask(bitmap),
                            bV5GreenMask = GetGreenMask(bitmap),
                            bV5AlphaMask = GetAlphaMask(bitmap),
                            bV5CSType = BitmapColorSpace.LCS_sRGB,
                            bV5Intent = BitmapIntent.LCS_GM_ABS_COLORIMETRIC
                        };
                        infoHeader.Init();

                        var imageData = new byte[infoHeader.bV5Size + infoHeader.bV5SizeImage];

                        fixed (byte* image = imageData)
                        {
                            Marshal.StructureToPtr(infoHeader, (IntPtr)image, false);
                            new Span<byte>(bytes, buffer.Length).CopyTo(new Span<byte>((image + infoHeader.bV5Size), buffer.Length));

                            return WriteBytesToHGlobal(ref hGlobal, imageData);
                        }
                    }
                }
            }
        }

        if (ClipboardFormatRegistry.PngSystemDataFormat.Equals(format)
            || ClipboardFormatRegistry.PngMimeDataFormat.Equals(format))
        {
            var bitmap = dataTransfer.TryGetValue(DataFormat.Bitmap);
            if (bitmap != null)
            {
                using var stream = new MemoryStream();
                bitmap.Save(stream);

                return WriteBytesToHGlobal(ref hGlobal, stream.ToArray().AsSpan());
            }
            return DV_E_FORMATETC;
        }

        if (format is DataFormat<string> stringFormat)
        {
            return dataTransfer.TryGetValue(stringFormat) is { } stringValue ?
                WriteStringToHGlobal(ref hGlobal, stringValue) :
                DV_E_FORMATETC;
        }

        if (format is DataFormat<byte[]> bytesFormat)
        {
            return dataTransfer.TryGetValue(bytesFormat) is { } bytes ?
                WriteBytesToHGlobal(ref hGlobal, bytes.AsSpan()) :
                DV_E_FORMATETC;
        }

        Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)
            ?.Log(null, "Unsupported data format {Format}", format);

        return DV_E_FORMATETC;
    }

    private static uint GetAlphaMask(Bitmap? bitmap)
    {
        return bitmap?.Format?.FormatEnum switch
        {
            PixelFormatEnum.Rgba8888 => 0xff000000,
            PixelFormatEnum.Bgra8888 => 0xff000000,
            PixelFormatEnum.Rgb565 => 0,
            _ => throw new NotSupportedException()
        };
    }

    private static uint GetGreenMask(Bitmap? bitmap)
    {
        return bitmap?.Format?.FormatEnum switch
        {
            PixelFormatEnum.Rgba8888 => 0x0000ff00,
            PixelFormatEnum.Bgra8888 => 0x0000ff00,
            PixelFormatEnum.Rgb565 => 0b0000011111100000,
            _ => throw new NotSupportedException()
        };
    }

    private static uint GetBlueMask(Bitmap? bitmap)
    {
        return bitmap?.Format?.FormatEnum switch
        {
            PixelFormatEnum.Rgba8888 => 0x00ff0000,
            PixelFormatEnum.Bgra8888 => 0x000000ff,
            PixelFormatEnum.Rgb565 => 0b1111100000000000,
            _ => throw new NotSupportedException()
        };
    }

    private static uint GetRedMask(Bitmap? bitmap)
    {
        return bitmap?.Format?.FormatEnum switch
        {
            PixelFormatEnum.Rgba8888 => 0x000000ff,
            PixelFormatEnum.Bgra8888 => 0x00ff0000,
            PixelFormatEnum.Rgb565 => 0b0000000000011111,
            _ => throw new NotSupportedException()
        };
    }

    public unsafe static uint WriteDataToGdi(IDataTransfer dataTransfer, DataFormat format, ref IntPtr hGlobalBitmap)
    {
        if (ClipboardFormatRegistry.HBitmapDataFormat.Equals(format))
        {
            var bitmap = dataTransfer.TryGetValue(DataFormat.Bitmap);
            if (bitmap != null)
            {
                var pixelSize = bitmap.PixelSize;
                var bpp = bitmap.Format?.BitsPerPixel ?? 0;
                var stride = (bpp / 8) * pixelSize.Width;
                var buffer = new byte[stride * pixelSize.Height];
                fixed (byte* bytes = buffer)
                {
                    bitmap.CopyPixels(new PixelRect(pixelSize), (IntPtr)bytes, buffer.Length, stride);

                    using var screenDc = GdiDC.Get();
                    if (screenDc.IsInvalid)
                        return DV_E_FORMATETC;

                    using var sourceDc = GdiDC.CreateCompatible(screenDc);
                    if (sourceDc.IsInvalid)
                        return DV_E_FORMATETC;

                    using var destDc = GdiDC.CreateCompatible(screenDc);
                    if (destDc.IsInvalid)
                        return DV_E_FORMATETC;

                    var bitmapInfoHeader = new BITMAPV5HEADER()
                    {
                        bV5Width = pixelSize.Width,
                        bV5Height = -pixelSize.Height,
                        bV5Planes = 1,
                        bV5BitCount = (ushort)bpp,
                        bV5Compression = BitmapCompressionMode.BI_BITFIELDS,
                        bV5SizeImage = (uint)buffer.Length,
                        bV5RedMask = GetRedMask(bitmap),
                        bV5BlueMask = GetBlueMask(bitmap),
                        bV5GreenMask = GetGreenMask(bitmap),
                        bV5AlphaMask = GetAlphaMask(bitmap),
                        bV5CSType = BitmapColorSpace.LCS_sRGB,
                        bV5Intent = BitmapIntent.LCS_GM_ABS_COLORIMETRIC,
                    };

                    bitmapInfoHeader.Init();

                    using var section = new GdiObject(
                        CreateDIBSection(sourceDc, bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0));
                    if (section.IsInvalid)
                        return DV_E_FORMATETC;

                    Marshal.Copy(buffer, 0, lbBits, buffer.Length);

                    // A bitmap compatible with a new memory DC is monochrome, so use the screen DC.
                    var hbitmap = CreateCompatibleBitmap(screenDc, pixelSize.Width, pixelSize.Height);
                    if (hbitmap == IntPtr.Zero)
                        return DV_E_FORMATETC;

                    bool copied;
                    using (sourceDc.Select(section))
                    using (destDc.Select(hbitmap))
                        copied = BitBlt(destDc, 0, 0, pixelSize.Width, pixelSize.Height, sourceDc, 0, 0, SRCCOPY);

                    if (!copied)
                    {
                        DeleteObject(hbitmap);
                        return DV_E_FORMATETC;
                    }

                    GdiFlush();

                    hGlobalBitmap = hbitmap;
                    return (uint)HRESULT.S_OK;
                }
            }
        }

        Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)
            ?.Log(null, "Unsupported gdi data format {Format}", format);

        return DV_E_FORMATETC;
    }

    private static unsafe uint WriteStringToHGlobal(ref IntPtr hGlobal, string data)
    {
        var requiredSize = (data.Length + 1) * sizeof(char);

        if (hGlobal == IntPtr.Zero)
            hGlobal = GlobalAlloc(GlobalAllocFlags.GHND, requiredSize);

        var availableSize = GlobalSize(hGlobal).ToInt64();
        if (requiredSize > availableSize)
            return STG_E_MEDIUMFULL;

        var destPtr = GlobalLock(hGlobal);
        try
        {
            fixed (char* sourcePtr = data)
            {
                Buffer.MemoryCopy(sourcePtr, (void*)destPtr, requiredSize, requiredSize);
            }

            return (uint)HRESULT.S_OK;
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    private static unsafe uint WriteFileNamesToHGlobal(ref IntPtr hGlobal, IEnumerable<string> fileNames)
    {
        var buffer = StringBuilderCache.Acquire();

        foreach (var fileName in fileNames)
        {
            buffer.Append(fileName);
            buffer.Append('\0');
        }

        buffer.Append('\0');

        var dropFiles = new DROPFILES
        {
            pFiles = (uint)sizeof(DROPFILES),
            pt = default,
            fNC = 0,
            fWide = 1
        };

        var requiredSize = sizeof(DROPFILES) + buffer.Length * sizeof(char);
        if (hGlobal == IntPtr.Zero)
            hGlobal = GlobalAlloc(GlobalAllocFlags.GHND, requiredSize);

        var availableSize = GlobalSize(hGlobal).ToInt64();
        if (requiredSize > availableSize)
        {
            StringBuilderCache.Release(buffer);
            return STG_E_MEDIUMFULL;
        }

        var ptr = GlobalLock(hGlobal);
        try
        {
            var data = StringBuilderCache.GetStringAndRelease(buffer);
            var destSpan = new Span<byte>((void*)ptr, requiredSize);
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(destSpan, in dropFiles);
#else
                MemoryMarshal.Write(destSpan, ref dropFiles);
#endif

            fixed (char* sourcePtr = data)
            {
                var sourceSpan = MemoryMarshal.AsBytes(new Span<char>(sourcePtr, data.Length));
                sourceSpan.CopyTo(destSpan.Slice(sizeof(DROPFILES)));
            }

            return (uint)HRESULT.S_OK;
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    private static unsafe uint WriteBytesToHGlobal(ref IntPtr hGlobal, ReadOnlySpan<byte> data)
    {
        var requiredSize = data.Length;

        if (hGlobal == IntPtr.Zero)
            hGlobal = GlobalAlloc(GlobalAllocFlags.GHND, requiredSize);

        var available = GlobalSize(hGlobal).ToInt64();
        if (requiredSize > available)
            return STG_E_MEDIUMFULL;

        var destPtr = GlobalLock(hGlobal);
        try
        {
            data.CopyTo(new Span<byte>((void*)destPtr, requiredSize));
            return (uint)HRESULT.S_OK;
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }
}
