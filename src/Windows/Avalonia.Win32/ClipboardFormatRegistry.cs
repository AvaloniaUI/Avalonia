using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Input.Platform;
using Avalonia.Win32.Interop;
using Avalonia.Utilities;

namespace Avalonia.Win32
{
    internal static class ClipboardFormatRegistry
    {
        private const int MaxFormatNameLength = 260;

        private static readonly List<(DataFormat Format, ushort Id)> s_formats = [];

        static ClipboardFormatRegistry()
        {
            AddDataFormat(DataFormat.Text, (ushort)UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT);
            AddDataFormat(DataFormat.Text, (ushort)UnmanagedMethods.ClipboardFormat.CF_TEXT);
            AddDataFormat(DataFormat.File, (ushort)UnmanagedMethods.ClipboardFormat.CF_HDROP);
        }

        private static void AddDataFormat(DataFormat format, ushort id)
            => s_formats.Add((format, id));

        private static string GetFormatSystemName(ushort id)
        {
            var buffer = StringBuilderCache.Acquire(MaxFormatNameLength);
            if (UnmanagedMethods.GetClipboardFormatName(id, buffer, buffer.Capacity) > 0)
                return StringBuilderCache.GetStringAndRelease(buffer);
            return $"Unknown_Format_{id}";
        }

        public static DataFormat GetFormatById(ushort id)
        {
            lock (s_formats)
            {
                for (var i = 0; i < s_formats.Count; ++i)
                {
                    if (s_formats[i].Id == id)
                        return s_formats[i].Format;
                }

                var systemName = GetFormatSystemName(id);
                var format = DataFormat.FromSystemName(systemName);
                AddDataFormat(format, id);
                return format;
            }
        }

        public static ushort GetFormatId(DataFormat format)
        {
            lock (s_formats)
            {
                for (var i = 0; i < s_formats.Count; ++i)
                {
                    if (s_formats[i].Format.Equals(format))
                        return s_formats[i].Id;
                }

                var result = UnmanagedMethods.RegisterClipboardFormat(format.SystemName);
                if (result == 0)
                    throw new Win32Exception();

                var id = (ushort)result;
                AddDataFormat(format, id);
                return id;
            }
        }

    }
}
