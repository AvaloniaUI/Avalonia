using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public const string PngFormatMimeType = "image/png";
        public const string PngFormatSystemType = "PNG";
        public const string BitmapFormat = "CF_BITMAP";
        public const string DibFormat = "CF_DIB";
        public const string DibV5Format = "CF_DIBV5";
        private static readonly List<(DataFormat Format, ushort Id)> s_formats = [];

        public static DataFormat PngSystemDataFormat = DataFormat.FromSystemName<Bitmap>(PngFormatSystemType, AppPrefix);
        public static DataFormat PngMimeDataFormat = DataFormat.FromSystemName<Bitmap>(PngFormatMimeType, AppPrefix);
        public static DataFormat HBitmapDataFormat = DataFormat.FromSystemName<Bitmap>(BitmapFormat, AppPrefix);
        public static DataFormat DibDataFormat = DataFormat.FromSystemName<Bitmap>(DibFormat, AppPrefix);
        public static DataFormat DibV5DataFormat = DataFormat.FromSystemName<Bitmap>(DibV5Format, AppPrefix);

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
            if (Enum.IsDefined(typeof(UnmanagedMethods.ClipboardFormat), (int)id))
                return Enum.GetName(typeof(UnmanagedMethods.ClipboardFormat), (int)id)!;
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
                var format = DataFormat.FromSystemName<byte[]>(systemName, AppPrefix);
                AddDataFormat(format, id);
                return format;
            }
        }

        public static ushort GetFormatId(DataFormat format)
        {
            lock (s_formats)
            {
                if (DataFormat.Bitmap.Equals(format))
                {
                    (DataFormat, ushort)? pngFormat = null;
                    (DataFormat, ushort)? dibFormat = null;

                    foreach (var currentFormat in s_formats)
                    {
                        if (currentFormat.Id == (ushort)UnmanagedMethods.ClipboardFormat.CF_DIB)
                            dibFormat = currentFormat;
                        else if (currentFormat.Format.Identifier == PngFormatMimeType)
                            pngFormat = currentFormat;
                    }
                    var imageFormatId = pngFormat?.Item2 ?? dibFormat?.Item2 ?? 0;

                    if (imageFormatId != 0)
                    {
                        return imageFormatId;
                    }
                }

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
