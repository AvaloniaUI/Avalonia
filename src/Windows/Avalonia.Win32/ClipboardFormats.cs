using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Avalonia.Input;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    static class ClipboardFormats
    {
        private const int MAX_FORMAT_NAME_LENGTH = 260;

        class ClipboardFormat
        {
            public short Format { get; private set; }
            public string Name { get; private set; }
            public short[] Synthesized { get; private set; }

            public ClipboardFormat(string name, short format, params short[] synthesized)
            {
                Format = format;
                Name = name;
                Synthesized = synthesized;
            }
        }

        private static readonly List<ClipboardFormat> FormatList = new List<ClipboardFormat>()
        {
            new ClipboardFormat(DataFormats.Text, (short)UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, (short)UnmanagedMethods.ClipboardFormat.CF_TEXT),
            new ClipboardFormat(DataFormats.FileNames, (short)UnmanagedMethods.ClipboardFormat.CF_HDROP),
        };


        private static string QueryFormatName(short format)
        {
            StringBuilder sb = new StringBuilder(MAX_FORMAT_NAME_LENGTH);
            if (UnmanagedMethods.GetClipboardFormatName(format, sb, sb.Capacity) > 0)
                return sb.ToString();
            return null;
        }

        public static string GetFormat(short format)
        {
            lock (FormatList)
            {
                var pd = FormatList.FirstOrDefault(f => f.Format == format || Array.IndexOf(f.Synthesized, format) >= 0);
                if (pd == null)
                {
                    string name = QueryFormatName(format);
                    if (string.IsNullOrEmpty(name))
                        name = string.Format("Unknown_Format_{0}", format);
                    pd = new ClipboardFormat(name, format);
                    FormatList.Add(pd);
                }
                return pd.Name;
            }
        }

        public static short GetFormat(string format)
        {
            lock (FormatList)
            {
                var pd = FormatList.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, format));
                if (pd == null)
                {
                    int id = UnmanagedMethods.RegisterClipboardFormat(format);
                    if (id == 0)
                        throw new Win32Exception();
                    pd = new ClipboardFormat(format, (short)id);
                    FormatList.Add(pd);
                }
                return pd.Format;
            }
        }
        

    }
}
