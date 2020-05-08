using System;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public class GlBasicInfoInterface : GlBasicInfoInterface<object>
    {
        public GlBasicInfoInterface(Func<string, IntPtr> getProcAddress) : base(getProcAddress, null)
        {
        }

        public GlBasicInfoInterface(Func<Utf8Buffer, IntPtr> nativeGetProcAddress) : base(nativeGetProcAddress, null)
        {
        }
        
        public delegate void GlGetIntegerv(int name, out int rv);
        public delegate IntPtr GlGetString(int v);
    }
    
    public class GlBasicInfoInterface<TContextInfo> : GlInterfaceBase<TContextInfo>
    {
        public GlBasicInfoInterface(Func<string, IntPtr> getProcAddress, TContextInfo context) : base(getProcAddress, context)
        {
        }

        public GlBasicInfoInterface(Func<Utf8Buffer, IntPtr> nativeGetProcAddress, TContextInfo context) : base(nativeGetProcAddress, context)
        {
        }
        
        [GlEntryPoint("glGetIntegerv")]
        public GlBasicInfoInterface.GlGetIntegerv GetIntegerv { get; }
        
        
        [GlEntryPoint("glGetString")]
        public GlBasicInfoInterface.GlGetString GetStringNative { get; }

        public string GetString(int v)
        {
            var ptr = GetStringNative(v);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }
    }
}
