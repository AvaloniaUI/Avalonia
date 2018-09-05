using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Gtk3.Interop
{
    internal class Pixbuf : GObject, IWindowIconImpl
    {
        Pixbuf(IntPtr handle) : base(handle)
        {
            
        }

        public static Pixbuf NewFromFile(string filename)
        {
            using (var ub = new Utf8Buffer(filename))
            {
                IntPtr err;
                var rv = Native.GdkPixbufNewFromFile(ub, out err);
                if(rv != IntPtr.Zero)
                    return new Pixbuf(rv);
                throw new GException(err);
            }
        }

        public static unsafe Pixbuf NewFromBytes(byte[] data)
        {
            fixed (void* bytes = data)
            {
                using (var stream = Native.GMemoryInputStreamNewFromData(new IntPtr(bytes), new IntPtr(data.Length), IntPtr.Zero))
                {
                    IntPtr err;
                    var rv = Native.GdkPixbufNewFromStream(stream, IntPtr.Zero, out err);
                    if (rv != IntPtr.Zero)
                        return new Pixbuf(rv);
                    throw new GException(err);
                }
            }
        }

        public static Pixbuf NewFromStream(Stream s)
        {
            if (s is MemoryStream)
                return NewFromBytes(((MemoryStream) s).ToArray());
            var ms = new MemoryStream();
            s.CopyTo(ms);
            return NewFromBytes(ms.ToArray());
        }

        public void Save(Stream outputStream)
        {
            IntPtr buffer, bufferLen, error;
            using (var png = new Utf8Buffer("png"))
                if (!Native.GdkPixbufSaveToBufferv(this, out buffer, out bufferLen, png,
                    IntPtr.Zero, IntPtr.Zero, out error))
                    throw new GException(error);
            var data = new byte[bufferLen.ToInt32()];
            Marshal.Copy(buffer, data, 0, bufferLen.ToInt32());
            Native.GFree(buffer);
            outputStream.Write(data, 0, data.Length);
        }
    }
}
