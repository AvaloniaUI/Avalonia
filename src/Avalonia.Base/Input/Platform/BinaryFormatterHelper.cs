using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Avalonia.Compatibility;
using Avalonia.Logging;

namespace Avalonia.Input.Platform;

// TODO12: remove
[Obsolete("Remove in v12")]
internal static class BinaryFormatterHelper
{
    // Compatibility with WinForms + WPF...
    private static ReadOnlySpan<byte> SerializedObjectGuid
        => [
            // FD9EA796-3B13-4370-A679-56106BB288FB
            0x96, 0xa7, 0x9e, 0xfd,
            0x13, 0x3b,
            0x70, 0x43,
            0xa6, 0x79, 0x56, 0x10, 0x6b, 0xb2, 0x88, 0xfb
        ];

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    public static byte[]? TrySerializeUsingBinaryFormatter(object data, DataFormat dataFormat)
    {
        if (!OperatingSystemEx.IsWindows())
            return null;

        Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(
            null,
            "Using BinaryFormatter to serialize data format {Format}. This won't be supported in Avalonia v12. Prefer passing a byte[] or Stream instead.",
            dataFormat);

        var stream = new MemoryStream();
        var serializedGuid = SerializedObjectGuid;

#if NET6_0_OR_GREATER
        stream.Write(serializedGuid);
#else
        stream.Write(serializedGuid.ToArray(), 0, serializedGuid.Length);
#endif

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        new BinaryFormatter().Serialize(stream, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

        return stream.GetBuffer();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for WinForms drag and drop compatibility")]
    public static object? TryDeserializeUsingBinaryFormatter(byte[]? bytes)
    {
        var serializedObjectGuid = SerializedObjectGuid;

        // Our Win32 backend used to automatically serialize/deserialize objects using the BinaryFormatter.
        // Only keep that behavior for compatibility with IDataObject.
        if (OperatingSystemEx.IsWindows() && bytes is not null && bytes.AsSpan().StartsWith(serializedObjectGuid))
        {
            using var stream = new MemoryStream(bytes);
            stream.Position = serializedObjectGuid.Length;

#pragma warning disable SYSLIB0011 // Type or member is obsolete
            return new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        }

        return null;
    }
}
