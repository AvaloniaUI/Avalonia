using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Gtk3.Interop
{
    class Utf8Buffer : SafeHandle
    {
        private GCHandle _gchandle;
        private byte[] _data;
            
        public Utf8Buffer(string s) : base(IntPtr.Zero, true)
        {
            if (s == null)
                return;
            _data = Encoding.UTF8.GetBytes(s);
            _gchandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            handle = _gchandle.AddrOfPinnedObject();
        }

        public int ByteLen => _data.Length;

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                handle = IntPtr.Zero;
                _data = null;
                _gchandle.Free();
            }
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static unsafe string StringFromPtr(IntPtr s)
        {
            var pstr = (byte*)s;
            if (pstr == null)
                return null;
            int len;
            for (len = 0; pstr[len] != 0; len++) ;
            var bytes = new byte[len];
            Marshal.Copy(s, bytes, 0, len);

            return Encoding.UTF8.GetString(bytes, 0, len);
        }
    }
}
