using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Native.Interop
{
    partial interface IAvgString
    {
        public string String { get; }
        public byte[] Bytes { get; }
    }

    partial interface IAvgStringArray
    {
        string[] ToStringArray();
    }

    public class AvgString : IAvgString
    {
        private IntPtr _native;
        private int _nativeLen;

        public AvgString(string s) => String = s;

        public string String { get; }
        public byte[] Bytes => Encoding.UTF8.GetBytes(String);

        public void Dispose()
        {
            
        }
        
        public unsafe void* Pointer()
        {
            EnsureNative();
            return _native.ToPointer();
        }

        public int Length()
        {
            EnsureNative();
            return _nativeLen;
        }

        private unsafe void EnsureNative()
        {
            if (string.IsNullOrEmpty(String))
                return;
            if (_native == IntPtr.Zero)
            {
                _nativeLen = Encoding.UTF8.GetByteCount(String);
                _native = Marshal.AllocHGlobal(_nativeLen + 1);
                var ptr = (byte*)_native.ToPointer();
                fixed (char* chars = String)
                    Encoding.UTF8.GetBytes(chars, String.Length, ptr, _nativeLen);
                ptr[_nativeLen] = 0;
            }
        }
    }
}
namespace Avalonia.Native.Interop.Impl
{
    public unsafe partial class __MicroComIAvgStringProxy 
    {
        private string _managed;
        private byte[] _bytes;

        public string String
        {
            get
            {
                if (_managed == null)
                {
                    var ptr = Pointer();
                    if (ptr == null)
                        return null;
                    _managed = System.Text.Encoding.UTF8.GetString((byte*)ptr, Length());
                }

                return _managed;
            }
        }

        public byte[] Bytes
        {
            get
            {
                if (_bytes == null)
                {
                    _bytes = new byte[Length()];
                    Marshal.Copy(new IntPtr(Pointer()), _bytes, 0, _bytes.Length);
                }

                return _bytes;
            }
        }

        public override string ToString() => String;
    }
}
