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
        
        public delegate void bGlGetIntegerv(uint name, out uint rv);
        public delegate IntPtr bGlGetString(uint v);
        public delegate IntPtr bGlGetStringi(uint v, uint v1);
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
        public GlBasicInfoInterface.bGlGetIntegerv bGetIntegerv { get; }        
        
        [GlEntryPoint("glGetString")]
        public GlBasicInfoInterface.bGlGetString bGetStringNative { get; }
        
        [GlEntryPoint("glGetStringi")]
        public GlBasicInfoInterface.bGlGetStringi bGetStringiNative { get; }

        public string bGetString(uint v)
        {
            var ptr = bGetStringNative(v);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }
        
        public string bGetString(uint v, uint index)
        {
            var ptr = bGetStringiNative(v, index);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }

        public List<string> GetExtensions()
        {
            var sp = bGetString(GlConsts.GL_EXTENSIONS);
            if (sp != null)
                return sp.Split(' ').ToList();
            bGetIntegerv(GlConsts.GL_NUM_EXTENSIONS, out uint count);
            var rv = new List<string>((int)count);
            for (uint c = 0; c < count; c++)
                rv.Add(bGetString(GlConsts.GL_EXTENSIONS, c));
            return rv;
        }
    }
}
