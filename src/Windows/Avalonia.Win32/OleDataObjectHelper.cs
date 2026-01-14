using System;
using System.Collections.Generic;
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

    public static FORMATETC ToFormatEtc(this DataFormat format, bool isGdi = false)
        => new()
        {
            cfFormat = ClipboardFormatRegistry.GetFormatId(format),
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            ptd = IntPtr.Zero,
            lindex = -1,
            tymed = isGdi ? TYMED.TYMED_GDI : TYMED.TYMED_HGLOBAL
        };

    public static unsafe object? TryGet(this Win32Com.IDataObject oleDataObject, DataFormat format)
    {
        var formatEtc = format.ToFormatEtc();

        if (oleDataObject.QueryGetData(&formatEtc) != (uint)HRESULT.S_OK)
            return null;

        var medium = new STGMEDIUM();
        var result = oleDataObject.GetData(&formatEtc, &medium);
        if (result != (uint)HRESULT.S_OK)
        {
            if (result == DV_E_TYMED)
            {
                formatEtc.tymed = TYMED.TYMED_GDI;

                if (oleDataObject.GetData(&formatEtc, &medium) != (uint)HRESULT.S_OK)
                {
                    return null;
                }
            }
            else
                return null;
        }

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

            IntPtr destHdc = IntPtr.Zero, compatDc = IntPtr.Zero, section = IntPtr.Zero, sourceHdc = IntPtr.Zero, srcCompatHdc = IntPtr.Zero;

            try
            {
                destHdc = GetDC(IntPtr.Zero);
                if (destHdc == IntPtr.Zero)
                    return null;

                compatDc = CreateCompatibleDC(destHdc);
                if (compatDc == IntPtr.Zero)
                    return null;

                section = CreateDIBSection(compatDc, ref bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0);
                if (section == IntPtr.Zero)
                    return null;

                SelectObject(compatDc, section);
                sourceHdc = GetDC(IntPtr.Zero);
                if (sourceHdc == IntPtr.Zero)
                    return null;

                srcCompatHdc = CreateCompatibleDC(sourceHdc);
                if (srcCompatHdc == IntPtr.Zero)
                    return null;

                SelectObject(srcCompatHdc, bitmapHandle);

                if (StretchBlt(compatDc, 0, bitmapInfoHeader.biHeight, bitmapInfoHeader.biWidth, -bitmapInfoHeader.biHeight, srcCompatHdc, 0, 0, bitmap.bmWidth, bitmap.bmHeight, SRCCOPY) != 0)
                    return new Bitmap(Platform.PixelFormats.Bgra8888,
                        Platform.AlphaFormat.Opaque,
                        lbBits,
                        new PixelSize(bitmapInfoHeader.biWidth, bitmapInfoHeader.biHeight),
                        new Vector(96, 96),
                        bitmapInfoHeader.biWidth * 4);
            }
            finally
            {
                if (sourceHdc != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, sourceHdc);

                if (srcCompatHdc != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, srcCompatHdc);

                if (compatDc != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, compatDc);

                if (destHdc != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, destHdc);

                if (section != IntPtr.Zero)
                    DeleteObject(section);
            }
            return null;
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
                fixed (byte* ptr = data)
                {
                    var bitmapInfo = Marshal.PtrToStructure<BITMAPINFO>((IntPtr)ptr);

                    var bitmapInfoHeader = new BITMAPINFOHEADER()
                    {
                        biWidth = bitmapInfo.biWidth,
                        biHeight = bitmapInfo.biHeight,
                        biPlanes = bitmapInfo.biPlanes,
                        biBitCount = 32,
                        biCompression = 0,
                        biSizeImage = (uint)(bitmapInfo.biWidth * 4 * Math.Abs(bitmapInfo.biHeight))
                    };

                    bitmapInfoHeader.Init();

                    IntPtr hdc = IntPtr.Zero, compatDc = IntPtr.Zero, section = IntPtr.Zero;
                    try
                    {
                        hdc = GetDC(IntPtr.Zero);
                        if (hdc == IntPtr.Zero)
                            return null;

                        compatDc = CreateCompatibleDC(hdc);
                        if (compatDc == IntPtr.Zero)
                            return null;

                        section = CreateDIBSection(compatDc, ref bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0);
                        if (section == IntPtr.Zero)
                            return null;

                        SelectObject(compatDc, section);
                        if (StretchDIBits(compatDc,
                                0,
                                bitmapInfo.biHeight,
                                bitmapInfo.biWidth,
                                -bitmapInfo.biHeight,
                                0,
                                0,
                                bitmapInfoHeader.biWidth,
                                bitmapInfoHeader.biHeight,
                                (IntPtr)(ptr + bitmapInfo.biSize),
                                ref bitmapInfo,
                                0,
                                SRCCOPY
                                ) != 0)
                            return new Bitmap(Platform.PixelFormats.Bgra8888,
                                Platform.AlphaFormat.Opaque,
                                lbBits,
                                new PixelSize(bitmapInfoHeader.biWidth, bitmapInfoHeader.biHeight),
                                new Vector(96, 96),
                                bitmapInfoHeader.biWidth * 4);
                    }
                    finally
                    {
                        if (section != IntPtr.Zero)
                            DeleteObject(section);

                        if (compatDc != IntPtr.Zero)
                            ReleaseDC(IntPtr.Zero, compatDc);

                        if (hdc != IntPtr.Zero)
                            ReleaseDC(IntPtr.Zero, hdc);
                    }
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

                    IntPtr hdc = IntPtr.Zero, compatDc = IntPtr.Zero, desDc = IntPtr.Zero, hbitmap = IntPtr.Zero, section = IntPtr.Zero;
                    try
                    {
                        hdc = GetDC(IntPtr.Zero);
                        if (hdc == IntPtr.Zero)
                            return DV_E_FORMATETC;

                        compatDc = CreateCompatibleDC(hdc);
                        if (compatDc == IntPtr.Zero)
                            return DV_E_FORMATETC;

                        desDc = CreateCompatibleDC(hdc);
                        if (desDc == IntPtr.Zero)
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

                        section = CreateDIBSection(compatDc, bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0);
                        if (section == IntPtr.Zero)
                            return DV_E_FORMATETC;

                        SelectObject(compatDc, section);

                        Marshal.Copy(buffer, 0, lbBits, buffer.Length);

                        hbitmap = CreateCompatibleBitmap(desDc, pixelSize.Width, pixelSize.Height);

                        SelectObject(desDc, hbitmap);

                        if (!BitBlt(desDc, 0, 0, pixelSize.Width, pixelSize.Height, compatDc, 0, 0, SRCCOPY))
                        {
                            return DV_E_FORMATETC;
                        }

                        hGlobalBitmap = hbitmap;

                        GdiFlush();

                        return (uint)HRESULT.S_OK;
                    }
                    finally
                    {
                        SelectObject(compatDc, IntPtr.Zero);
                        SelectObject(desDc, IntPtr.Zero);

                        if (desDc != IntPtr.Zero)
                            ReleaseDC(IntPtr.Zero, desDc);

                        if (compatDc != IntPtr.Zero)
                            ReleaseDC(IntPtr.Zero, compatDc);

                        if (hdc != IntPtr.Zero)
                            ReleaseDC(IntPtr.Zero, hdc);
                    }
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
