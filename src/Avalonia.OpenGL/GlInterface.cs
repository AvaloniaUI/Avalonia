using System;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);
    
    public class GlInterface : GlInterfaceBase
    {
        public string Version { get; }
        public string Vendor { get; }
        public string Renderer { get; }

        public GlInterface(Func<string, bool, IntPtr> getProcAddress) : base(getProcAddress)
        {
            Version = GetString(GlConsts.GL_VERSION);
            Renderer = GetString(GlConsts.GL_RENDERER);
            Vendor = GetString(GlConsts.GL_VENDOR);
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

        public delegate void GlClearColor(float r, float g, float b, float a);
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
        public GlGetString GetStringNative { get; }

        public string GetString(int v)
        {
            var ptr = GetStringNative(v);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }

        public delegate void GlGetIntegerv(int name, out int rv);
        [GlEntryPoint("glGetIntegerv")]
        public GlGetIntegerv GetIntegerv { get; }

        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
