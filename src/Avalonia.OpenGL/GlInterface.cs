using System;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);
    
    public unsafe class GlInterface : GlInterfaceBase
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

        public delegate void GlGenFramebuffers(int count, int[] res);
        [GlEntryPoint("glGenFramebuffers")]
        public GlGenFramebuffers GenFramebuffers { get; }
        
        public delegate void GlBindFramebuffer(int target, int fb);
        [GlEntryPoint("glBindFramebuffer")]
        public GlBindFramebuffer BindFramebuffer { get; }
        
        public delegate int GlCheckFramebufferStatus(int target);
        [GlEntryPoint("glCheckFramebufferStatus")]
        public GlCheckFramebufferStatus CheckFramebufferStatus { get; }
        
        public delegate void GlGenRenderbuffers(int count, int[] res);
        [GlEntryPoint("glGenRenderbuffers")]
        public GlGenRenderbuffers GenRenderbuffers { get; }
        
        public delegate void GlBindRenderbuffer(int target, int fb);
        [GlEntryPoint("glBindRenderbuffer")]
        public GlBindRenderbuffer BindRenderbuffer { get; }
        
        public delegate void GlRenderbufferStorage(int target, int internalFormat, int width, int height);
        [GlEntryPoint("glRenderbufferStorage")]
        public GlRenderbufferStorage RenderbufferStorage { get; }

        public delegate void GlFramebufferRenderbuffer(int target, int attachment,
            int renderbufferTarget, int renderbuffer);
        [GlEntryPoint("glFramebufferRenderbuffer")]
        public GlFramebufferRenderbuffer FramebufferRenderbuffer { get; }
        
        public delegate void GlGenTextures(int count, int[] res);
        [GlEntryPoint("glGenTextures")]
        public GlGenTextures GenTextures { get; }
        
        public delegate void GlBindTexture(int target, int fb);
        [GlEntryPoint("glBindTexture")]
        public GlBindTexture BindTexture { get; }

        public delegate void GlTexImage2D(int target, int level, int internalFormat, int width, int height, int border,
            int format, int type, IntPtr data);
        [GlEntryPoint("glTexImage2D")]
        public GlTexImage2D TexImage2D { get; }

        public delegate void GlTexParameteri(int target, int name, int value);
        [GlEntryPoint("glTexParameteri")]
        public GlTexParameteri TexParameteri { get; }

        public delegate void GlFramebufferTexture2D(int target, int attachment,
            int texTarget, int texture, int level);
        [GlEntryPoint("glFramebufferTexture2D")]
        public GlFramebufferTexture2D FramebufferTexture2D { get; }

        public delegate void GlDrawBuffers(int n, int[] bufs);
        [GlEntryPoint("glDrawBuffers")]
        public GlDrawBuffers DrawBuffers { get; }

        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
