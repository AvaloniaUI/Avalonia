using System;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);
    
    public class GlInterface : GlInterfaceBase
    {
        public string Version { get; }

        public GlInterface(Func<string, bool, IntPtr> getProcAddress) : base(getProcAddress)
        {
            var versionPtr = GetString(GlConsts.GL_VERSION);
            if (versionPtr != IntPtr.Zero)
                Version = Marshal.PtrToStringAnsi(versionPtr);
        }

        public GlInterface(Func<Utf8Buffer, IntPtr> n) : this(ConvertNative(n))
        {
            
        }

        public static GlInterface FromNativeUtf8GetProcAddress(Func<Utf8Buffer, IntPtr> getProcAddress) =>
            new GlInterface(getProcAddress);

        
        public T GetProcAddress<T>(string proc) => Marshal.GetDelegateForFunctionPointer<T>(GetProcAddress(proc));

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public delegate int GlGetError();
        [GlEntryPoint("glGetError")]
        public GlGetError GetError { get; }

        public delegate void GlClearStencil(int s);
        [GlEntryPoint("glClearStencil")]
        public GlClearStencil ClearStencil { get; }

        public delegate void GlClearColor(int r, int g, int b, int a);
        [GlEntryPoint("glClearColor")]
        public GlClearColor ClearColor { get; }

        public delegate void GlClear(int bits);
        [GlEntryPoint("glClear")]
        public GlClear Clear { get; }

        public delegate void GlViewport(int x, int y, int width, int height);
        [GlEntryPoint("glViewport")]
        public GlViewport Viewport { get; }
        
        [GlEntryPoint("glFlush")]
        public Action Flush { get; }

        public delegate IntPtr GlGetString(int v);
        [GlEntryPoint("glGetString")]
        public GlGetString GetString { get; }

        public delegate void GlGetIntegerv(int name, out int rv);
        [GlEntryPoint("glGetIntegerv")]
        public GlGetIntegerv GetIntegerv { get; }

        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
