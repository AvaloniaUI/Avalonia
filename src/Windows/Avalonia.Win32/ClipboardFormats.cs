using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Avalonia.Controls.DragDrop;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{   
    static class ClipboardFormats
    {
        class ClipboardFormat
        {
            public short Format { get; private set; }
            public string Name { get; private set; }

            public ClipboardFormat(string name, short format)
            {
                Format = format;
                Name = name;
            }
        }

        private static readonly List<ClipboardFormat> FormatList = new List<ClipboardFormat>()
        {
            new ClipboardFormat(DataFormats.Text, (short)UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT),
            new ClipboardFormat(DataFormats.FileNames, (short)UnmanagedMethods.ClipboardFormat.CF_HDROP),
        };


        private static string QueryFormatName(short format)
        {
            int len = UnmanagedMethods.GetClipboardFormatName(format, null, 0);
            if (len > 0)
            {
                StringBuilder sb = new StringBuilder(len);
                if (UnmanagedMethods.GetClipboardFormatName(format, sb, len) <= len)
                    return sb.ToString();
            }
            return null;
        }

        public static string GetFormat(short format)
        {
            lock (FormatList)
            {
                var pd = FormatList.FirstOrDefault(f => f.Format == format);
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