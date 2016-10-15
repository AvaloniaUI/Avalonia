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
            _data = Encoding.UTF8.GetBytes(s);
            _gchandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            handle = _gchandle.AddrOfPinnedObject();
        }

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
    }
}
