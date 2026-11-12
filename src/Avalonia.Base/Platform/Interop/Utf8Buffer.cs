#nullable enable

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Platform.Interop
{
    internal class Utf8Buffer : SafeHandle
    {
        private GCHandle _gcHandle;
        private byte[]? _data;
        private int _byteLen;

        public Utf8Buffer(string? s) : base(IntPtr.Zero, true)
        {
            if (s == null)
                return;
            _byteLen = Encoding.UTF8.GetByteCount(s);
            // The extra zero byte is the C string terminator.
            _data = new byte[_byteLen + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, _data, 0);
            _gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            handle = _gcHandle.AddrOfPinnedObject();
        }

        /// <summary>
        /// The length of the string in bytes, without the terminating zero.
        /// </summary>
        public int ByteLen => _byteLen;

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                handle = IntPtr.Zero;
                _data = null;
                _byteLen = 0;
                _gcHandle.Free();
            }
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static unsafe string? StringFromPtr(IntPtr s)
        {
            var pstr = (byte*)s;
            if (pstr == null)
                return null;
            int len;
            for (len = 0; pstr[len] != 0; len++) ;

            var bytes = ArrayPool<byte>.Shared.Rent(len);

            try
            {
                Marshal.Copy(s, bytes, 0, len);
                return Encoding.UTF8.GetString(bytes, 0, len);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public static implicit operator IntPtr(Utf8Buffer b) => b.handle;
        public static unsafe implicit operator byte*(Utf8Buffer b) => (byte*)b.handle;
    }
}
