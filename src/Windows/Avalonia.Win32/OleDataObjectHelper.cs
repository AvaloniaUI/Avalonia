using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Logging;
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
    public static FORMATETC ToFormatEtc(this DataFormat format)
        => new()
        {
            cfFormat = ClipboardFormatRegistry.GetFormatId(format),
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            ptd = IntPtr.Zero,
            lindex = -1,
            tymed = TYMED.TYMED_HGLOBAL
        };

    public static unsafe object? TryGet(this Win32Com.IDataObject _oleDataObject, DataFormat format)
    {
        var formatEtc = format.ToFormatEtc();

        if (_oleDataObject.QueryGetData(&formatEtc) != (uint)HRESULT.S_OK)
            return null;

        var medium = new STGMEDIUM();
        if (_oleDataObject.GetData(&formatEtc, &medium) != (uint)HRESULT.S_OK)
            return null;

        try
        {
            if (medium.tymed == TYMED.TYMED_HGLOBAL && medium.unionmember != IntPtr.Zero)
            {
                var hGlobal = medium.unionmember;
                return ReadDataFromHGlobal(format, hGlobal);
            }
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        return null;
    }

    public static object? ReadDataFromHGlobal(DataFormat format, IntPtr hGlobal)
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

        if(DataFormat.Image.Equals(format))
        {
            var data = ReadBytesFromHGlobal(hGlobal);
            using var stream = new MemoryStream(data);

            return new Avalonia.Media.Imaging.Bitmap(stream);
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

    public static uint WriteDataToHGlobal(IDataTransfer dataTransfer, DataFormat format, ref IntPtr hGlobal)
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
