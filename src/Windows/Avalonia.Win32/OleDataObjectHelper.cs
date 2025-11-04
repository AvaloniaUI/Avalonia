using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
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

    public static FORMATETC ToFormatEtc(this DataFormat format)
        => new()
        {
            cfFormat = ClipboardFormatRegistry.GetFormatId(format),
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            ptd = IntPtr.Zero,
            lindex = -1,
            tymed = TYMED.TYMED_HGLOBAL
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
            if (result == 0x80040069) // DV_E_TYMED
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
                    var bitmap = new BITMAP();
                    unsafe
                    {
                        var pBitmap = &bitmap;
                        GetObject(bitmapHandle, Marshal.SizeOf(bitmap), (IntPtr)pBitmap);

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

                        var destHdc = UnmanagedMethods.GetDC(IntPtr.Zero);
                        var compatDc = UnmanagedMethods.CreateCompatibleDC(destHdc);
                        var section = UnmanagedMethods.CreateDIBSection(compatDc, ref bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0);
                        SelectObject(compatDc, section);
                        var sourceHdc = UnmanagedMethods.GetDC(IntPtr.Zero);
                        var srcCompatHdc = UnmanagedMethods.CreateCompatibleDC(sourceHdc);
                        SelectObject(srcCompatHdc, bitmapHandle);

                        StretchBlt(compatDc, 0, bitmapInfoHeader.biHeight, bitmapInfoHeader.biWidth, -bitmapInfoHeader.biHeight, srcCompatHdc, 0, 0, bitmap.bmWidth, bitmap.bmHeight, SRCCOPY);
                        var avBitmap = new Bitmap(Platform.PixelFormats.Bgra8888,
                            Platform.AlphaFormat.Opaque,
                            lbBits,
                            new PixelSize(bitmapInfoHeader.biWidth, bitmapInfoHeader.biHeight),
                            new Vector(96, 96),
                            bitmapInfoHeader.biWidth * 4);

                        DeleteObject(section);

                        UnmanagedMethods.ReleaseDC(IntPtr.Zero, sourceHdc);
                        UnmanagedMethods.ReleaseDC(IntPtr.Zero, srcCompatHdc);
                        UnmanagedMethods.ReleaseDC(IntPtr.Zero, compatDc);
                        UnmanagedMethods.ReleaseDC(IntPtr.Zero, destHdc);

                        return avBitmap;
                    }
                }
            }
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        return null;
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

        if (DataFormat.Image.Equals(format))
        {
            if (formatEtc.cfFormat == (ushort)UnmanagedMethods.ClipboardFormat.CF_DIB)
            {
                var data = ReadBytesFromHGlobal(hGlobal);
                fixed (byte* ptr = data)
                {
                    var bitmapInfo = Marshal.PtrToStructure<UnmanagedMethods.BITMAPINFO>((IntPtr)ptr);

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

                    var hdc = UnmanagedMethods.GetDC(IntPtr.Zero);
                    var compatDc = UnmanagedMethods.CreateCompatibleDC(hdc);
                    var section = UnmanagedMethods.CreateDIBSection(compatDc, ref bitmapInfoHeader, 0, out var lbBits, IntPtr.Zero, 0);
                    SelectObject(compatDc, section);
                    var ret = UnmanagedMethods.StretchDIBits(compatDc,
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
                        );

                    var bitmap = new Bitmap(Platform.PixelFormats.Bgra8888,
                        Platform.AlphaFormat.Opaque,
                        lbBits,
                        new PixelSize(bitmapInfoHeader.biWidth, bitmapInfoHeader.biHeight), 
                        new Vector(96, 96),
                        bitmapInfoHeader.biWidth * 4);

                    DeleteObject(section);
                    UnmanagedMethods.ReleaseDC(IntPtr.Zero, compatDc);
                    UnmanagedMethods.ReleaseDC(IntPtr.Zero, hdc);
                    return bitmap;
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

        if (DataFormat.Image.Equals(format))
        {
            var bitmap = dataTransfer.TryGetValue(DataFormat.Image);
            if (bitmap != null)
            {
                var pixelSize = bitmap.PixelSize;
                var stride = ((bitmap.Format?.BitsPerPixel ?? 0) / 8) * pixelSize.Width;
                var buffer = new byte[stride * pixelSize.Height];
                fixed (byte* bytes = buffer)
                {
                    bitmap.CopyPixels(new PixelRect(pixelSize), (IntPtr)bytes, buffer.Length, stride);

                    var infoHeader = new BITMAPINFOHEADER()
                    {
                        biSizeImage = (uint)buffer.Length,
                        biWidth = pixelSize.Width,
                        biHeight = -pixelSize.Height,
                        biBitCount = (ushort)(bitmap.Format?.BitsPerPixel ?? 0),
                        biPlanes = 1,
                        biCompression = (uint)BitmapCompressionMode.BI_RGB
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
            }
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
