using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Input;
using Avalonia.Win32.Interop;
using Avalonia.Utilities;

namespace Avalonia.Win32
{
    internal static class ClipboardFormats
    {
        private const int MAX_FORMAT_NAME_LENGTH = 260;

        private class ClipboardFormat
        {
            public ushort Format { get; }
            public string Name { get; }
            public ushort[] Synthesized { get; }

            public ClipboardFormat(string name, ushort format, params ushort[] synthesized)
            {
                Format = format;
                Name = name;
                Synthesized = synthesized;
            }
        }

        private static readonly List<ClipboardFormat> s_formatList = new()
        {
            new ClipboardFormat(DataFormats.Text, (ushort)UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, (ushort)UnmanagedMethods.ClipboardFormat.CF_TEXT),
            new ClipboardFormat(DataFormats.Files, (ushort)UnmanagedMethods.ClipboardFormat.CF_HDROP),
#pragma warning disable CS0618 // Type or member is obsolete
            new ClipboardFormat(DataFormats.FileNames, (ushort)UnmanagedMethods.ClipboardFormat.CF_HDROP),
#pragma warning restore CS0618 // Type or member is obsolete
        };


        private static string? QueryFormatName(ushort format)
        {
            var sb = StringBuilderCache.Acquire(MAX_FORMAT_NAME_LENGTH);
            if (UnmanagedMethods.GetClipboardFormatName(format, sb, sb.Capacity) > 0)
                return StringBuilderCache.GetStringAndRelease(sb);
            return null;
        }

        public static string GetFormat(ushort format)
        {
            lock (s_formatList)
            {
                var pd = s_formatList.FirstOrDefault(f => f.Format == format || Array.IndexOf(f.Synthesized, format) >= 0);
                if (pd == null)
                {
                    string? name = QueryFormatName(format);
                    if (string.IsNullOrEmpty(name))
                        name = $"Unknown_Format_{format}";
                    pd = new ClipboardFormat(name, format);
                    s_formatList.Add(pd);
                }
                return pd.Name;
            }
        }

        public static ushort GetFormat(string format)
        {
            lock (s_formatList)
            {
                var pd = s_formatList.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, format));
                if (pd == null)
                {
                    int id = UnmanagedMethods.RegisterClipboardFormat(format);
                    if (id == 0)
                        throw new Win32Exception();
                    pd = new ClipboardFormat(format, (ushort)id);
                    s_formatList.Add(pd);
                }
                return pd.Format;
            }
        }
        

    }
}
