using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Utilities;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal static class ClipboardFormatRegistry
    {
        private const int MaxFormatNameLength = 260;
        private const string AppPrefix = "avn-app-fmt:";
        private static readonly List<(DataFormat Format, ushort Id)> s_formats = [];

        public static DataFormat PngSystemDataFormat = new DataFormat<Bitmap>(DataFormatKind.Platform, "PNG");
        public static DataFormat PngMimeDataFormat = new DataFormat<Bitmap>(DataFormatKind.Platform, "image/png");
        public static DataFormat HBitmapDataFormat = new DataFormat<Bitmap>(DataFormatKind.Platform, "CF_BITMAP");
        public static DataFormat DibDataFormat = new DataFormat<Bitmap>(DataFormatKind.Platform, "CF_DIB");
        public static DataFormat DibV5DataFormat = new DataFormat<Bitmap>(DataFormatKind.Platform, "CF_DIBV5");

        // Ordered from the most preferred to the least preferred
        public static DataFormat[] ImageFormats = [PngMimeDataFormat, PngSystemDataFormat, DibDataFormat, DibV5DataFormat, HBitmapDataFormat];

        static ClipboardFormatRegistry()
        {
            AddDataFormat(DataFormat.Text, (ushort)UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT);
            AddDataFormat(DataFormat.File, (ushort)UnmanagedMethods.ClipboardFormat.CF_HDROP);
            AddDataFormat(DibDataFormat, (ushort)UnmanagedMethods.ClipboardFormat.CF_DIB);
            AddDataFormat(DibV5DataFormat, (ushort)UnmanagedMethods.ClipboardFormat.CF_DIBV5);
            AddDataFormat(HBitmapDataFormat, (ushort)UnmanagedMethods.ClipboardFormat.CF_BITMAP);
        }

        private static void AddDataFormat(DataFormat format, ushort id)
            => s_formats.Add((format, id));

        private static string GetFormatSystemName(ushort id)
        {
            var buffer = StringBuilderCache.Acquire(MaxFormatNameLength);
            if (UnmanagedMethods.GetClipboardFormatName(id, buffer, buffer.Capacity) > 0)
                return StringBuilderCache.GetStringAndRelease(buffer);
            if (Enum.IsDefined(typeof(UnmanagedMethods.ClipboardFormat), id))
                return Enum.GetName(typeof(UnmanagedMethods.ClipboardFormat), id)!;
            return $"Unknown_Format_{id}";
        }

        public static DataFormat GetOrAddFormat(ushort id)
        {
            lock (s_formats)
            {
                for (var i = 0; i < s_formats.Count; ++i)
                {
                    if (s_formats[i].Id == id)
                        return s_formats[i].Format;
                }

                var systemName = GetFormatSystemName(id);
                var format = DataFormat.FromSystemName<byte[]>(systemName, AppPrefix);
                AddDataFormat(format, id);
                return format;
            }
        }

        public static ushort GetOrAddFormat(DataFormat format)
        {
            Debug.Assert(format != DataFormat.Bitmap); // Callers must pass an effective platform type

            lock (s_formats)
            {
                for (var i = 0; i < s_formats.Count; ++i)
                {
                    if (s_formats[i].Format.Equals(format))
                        return s_formats[i].Id;
                }

                var systemName = format.ToSystemName(AppPrefix);
                var result = UnmanagedMethods.RegisterClipboardFormat(systemName);
                if (result == 0)
                    throw new Win32Exception();

                var id = (ushort)result;
                AddDataFormat(format, id);
                return id;
            }
        }

    }
}
