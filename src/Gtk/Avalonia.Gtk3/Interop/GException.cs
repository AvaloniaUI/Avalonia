using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Gtk3.Interop
{
    public class GException : Exception
    {
        [StructLayout(LayoutKind.Sequential)]
        struct GError
        {
            UInt32 domain;
            int code;
            public IntPtr message;
        };

        static unsafe string GetError(IntPtr error)
        {
            if (error == IntPtr.Zero)
                return "Unknown error";
            return Utf8Buffer.StringFromPtr(((GError*) error)->message);
        }

        public GException(IntPtr error) : base(GetError(error))
        {
            
        }

    }
}
