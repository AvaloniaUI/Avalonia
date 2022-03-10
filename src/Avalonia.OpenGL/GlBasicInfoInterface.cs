using System;
using System.Collections.Generic;
using System.Linq;
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGetIntegerv(int name, out int rv);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GlGetString(int v);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GlGetStringi(int v, int v1);
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
        
        [GlEntryPoint("glGetStringi")]
        public GlBasicInfoInterface.GlGetStringi GetStringiNative { get; }

        public string GetString(int v)
        {
            var ptr = GetStringNative(v);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }
        
        public string GetString(int v, int index)
        {
            var ptr = GetStringiNative(v, index);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }

        public List<string> GetExtensions()
        {
            var sp = GetString(GlConsts.GL_EXTENSIONS);
            if (sp != null)
                return sp.Split(' ').ToList();
            GetIntegerv(GlConsts.GL_NUM_EXTENSIONS, out int count);
            var rv = new List<string>(count);
            for (var c = 0; c < count; c++)
                rv.Add(GetString(GlConsts.GL_EXTENSIONS, c));
            return rv;
        }
    }
}
