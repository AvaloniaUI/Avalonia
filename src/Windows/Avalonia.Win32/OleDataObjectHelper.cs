using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Utilities;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using STGMEDIUM = Avalonia.Win32.Interop.UnmanagedMethods.STGMEDIUM;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

/// <summary>
/// Contains helper methods to read from and write to an HGlobal, for <see cref="Win32Com.IDataObject"/> interop.
/// </summary>
internal static class OleDataObjectHelper
{
    // Compatibility with WinForms + WPF...
    // TODO12: remove
    private static ReadOnlySpan<byte> SerializedObjectGuid
        => [
            // FD9EA796-3B13-4370-A679-56106BB288FB
            0x96, 0xa7, 0x9e, 0xfd,
            0x13, 0x3b,
            0x70, 0x43,
            0xa6, 0x79, 0x56, 0x10, 0x6b, 0xb2, 0x88, 0xfb
        ];

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

        return DeserializeFromHGlobal(hGlobal);
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

    // TODO12: remove, use ReadBytesFromHGlobal instead
    private static unsafe object DeserializeFromHGlobal(IntPtr hGlobal)
    {
        var sourcePtr = GlobalLock(hGlobal);
        try
        {
            var size = (int)GlobalSize(hGlobal);
            var source = new ReadOnlySpan<byte>((void*)sourcePtr, size);

            if (source.StartsWith(SerializedObjectGuid))
            {
                using var stream = new UnmanagedMemoryStream((byte*)sourcePtr, size);
                stream.Position = SerializedObjectGuid.Length;
                return DeserializeUsingBinaryFormatter(stream);
            }

            return source.ToArray();
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    // TODO12: remove
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    private static object DeserializeUsingBinaryFormatter(UnmanagedMemoryStream stream)
    {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        return new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
    }

    public static uint WriteDataToHGlobal(object data, DataFormat format, ref IntPtr hGlobal)
    {
        if (DataFormat.Text.Equals(format))
            return WriteStringToHGlobal(ref hGlobal, Convert.ToString(data) ?? string.Empty);

        if (DataFormat.File.Equals(format))
        {
            var files = GetTypedData<IEnumerable<IStorageItem>>(data, format) ?? [];

            IEnumerable<string> fileNames = files
                .Select(StorageProviderExtensions.TryGetLocalPath)
                .Where(path => path is not null)!;

            return WriteFileNamesToHGlobal(ref hGlobal, fileNames);
        }

        switch (data)
        {
            case byte[] bytes:
                return WriteBytesToHGlobal(ref hGlobal, bytes.AsSpan());

            case Memory<byte> bytes:
                return WriteBytesToHGlobal(ref hGlobal, bytes.Span);

            case string str:
                return WriteStringToHGlobal(ref hGlobal, str);

            case Stream stream:
            {
                var length = (int)(stream.Length - stream.Position);
                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    stream.ReadExactly(buffer, 0, length);
                    return WriteBytesToHGlobal(ref hGlobal, buffer.AsSpan(0, length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            default:
            {
                // TODO12: remove the BinaryFormatter support
                var bytes = SerializeUsingBinaryFormatter(data, format);
                return WriteBytesToHGlobal(ref hGlobal, bytes);
            }
        }

        static T? GetTypedData<T>(object? data, DataFormat format) where T : class
            => data switch
            {
                null => null,
                T value => value,
                _ => throw new InvalidOperationException(
                    $"Expected a value of type {typeof(T)} for data format {format}, got {data.GetType()} instead.")
            };
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

    // TODO12: remove
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    private static ReadOnlySpan<byte> SerializeUsingBinaryFormatter(object data, DataFormat format)
    {
        Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(
            null,
            "Using BinaryFormatter to serialize data format {Format}, prefer passing a byte[] or Stream instead.",
            format);

        var stream = new MemoryStream();

#if NET6_0_OR_GREATER
        stream.Write(SerializedObjectGuid);
#else
            stream.Write(SerializedObjectGuid.ToArray(), 0, SerializedObjectGuid.Length);
#endif

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        new BinaryFormatter().Serialize(stream, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

        var buffer = stream.GetBuffer();
        return new ReadOnlySpan<byte>(buffer, 0, buffer.Length);
    }
}
