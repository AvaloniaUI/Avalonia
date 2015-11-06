using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Media;

namespace Perspex.Skia
{
    static class TypefaceCache
    {
        static readonly Dictionary<string, Dictionary<Style, IntPtr>>Cache = new Dictionary<string, Dictionary<Style, IntPtr>>();
        unsafe static IntPtr GetTypeface(string name, Style style)
        {
            if (name == null)
                name = "Arial";
            Dictionary<Style, IntPtr> entry;
            if (!Cache.TryGetValue(name, out entry))
                Cache[name] = entry = new Dictionary<Style, IntPtr>();
            IntPtr rv;
            if (!entry.TryGetValue(style, out rv))
            {
                var bytes = Encoding.ASCII.GetBytes(name);
                var buffer = new byte[bytes.Length + 1];
                bytes.CopyTo(buffer, 0);
                fixed (void* pname = buffer)
                {
                    entry[style] = rv = MethodTable.Instance.CreateTypeface(pname, (int)style);
                }
            }
            return rv;
        }

        [Flags]
        enum Style
        {
            Normal = 0,
            Bold = 0x01,
            Italic = 0x02,
            BoldItalic = 0x03
        };

        public static IntPtr GetTypeface(string name, FontStyle style, FontWeight weight)
        {
            Style sstyle = Style.Normal;
            if (style != FontStyle.Normal)
                sstyle |= Style.Italic;
            if(weight>FontWeight.Normal)
            sstyle |= Style.Bold;
            return GetTypeface(name, sstyle);
        }

    }
}