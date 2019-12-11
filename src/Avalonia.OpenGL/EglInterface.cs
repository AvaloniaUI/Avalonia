using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public class EglInterface : GlInterfaceBase
    {
        public EglInterface() : base(Load())
        {
            
        }

        public EglInterface(Func<Utf8Buffer,IntPtr> getProcAddress) : base(getProcAddress)
        {
            
        }
        
        public EglInterface(string library) : base(Load(library))
        {
        }

        [DllImport("libegl.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr eglGetProcAddress(string proc);
        
        static Func<string, bool, IntPtr> Load()
        {
            var os = AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem;
            if(os == OperatingSystemType.Linux || os == OperatingSystemType.Android)
                return Load("libEGL.so.1");
            if (os == OperatingSystemType.WinNT)
            {
                var disp = eglGetProcAddress("eglGetPlatformDisplayEXT");
                if (disp == IntPtr.Zero)
                    throw new OpenGlException("libegl.dll doesn't have eglGetPlatformDisplayEXT entry point");
                return (name, optional) =>
                {
                    var r = eglGetProcAddress(name);
                    if (r == IntPtr.Zero && !optional)
                        throw new OpenGlException($"Entry point {r} is not found");
                    return r;
                };
            }

            throw new PlatformNotSupportedException();
        }

        static Func<string, bool, IntPtr> Load(string library)
        {
            var dyn = AvaloniaLocator.Current.GetService<IDynamicLibraryLoader>();
            var lib = dyn.LoadLibrary(library);
            return (s, o) => dyn.GetProcAddress(lib, s, o);
        }

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public delegate int EglGetError();
        [GlEntryPoint("eglGetError")]
        public EglGetError GetError { get; }

        public delegate IntPtr EglGetDisplay(IntPtr nativeDisplay);
        [GlEntryPoint("eglGetDisplay")]
        public EglGetDisplay GetDisplay { get; }
        
        public delegate IntPtr EglGetPlatformDisplayEXT(int platform, IntPtr nativeDisplay, int[] attrs);
        [GlEntryPoint("eglGetPlatformDisplayEXT", true)]
        public EglGetPlatformDisplayEXT GetPlatformDisplayEXT { get; }

        public delegate bool EglInitialize(IntPtr display, out int major, out int minor);
        [GlEntryPoint("eglInitialize")]
        public EglInitialize Initialize { get; }        
        
        public delegate IntPtr EglGetProcAddress(Utf8Buffer proc);
        [GlEntryPoint("eglGetProcAddress")]
        public EglGetProcAddress GetProcAddress { get; }

        public delegate bool EglBindApi(int api);
        [GlEntryPoint("eglBindAPI")]
        public EglBindApi BindApi { get; }

        public delegate bool EglChooseConfig(IntPtr display, int[] attribs,
            out IntPtr surfaceConfig, int numConfigs, out int choosenConfig);
        [GlEntryPoint("eglChooseConfig")]
        public EglChooseConfig ChooseConfig { get; }

        public delegate IntPtr EglCreateContext(IntPtr display, IntPtr config,
            IntPtr share, int[] attrs);
        [GlEntryPoint("eglCreateContext")]
        public EglCreateContext CreateContext { get; }

        public delegate IntPtr EglCreatePBufferSurface(IntPtr display, IntPtr config, int[] attrs);
        [GlEntryPoint("eglCreatePbufferSurface")]
        public EglCreatePBufferSurface CreatePBufferSurface { get; }

        public delegate bool EglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
        [GlEntryPoint("eglMakeCurrent")]
        public EglMakeCurrent MakeCurrent { get; }

        public delegate void EglDisplaySurfaceVoidDelegate(IntPtr display, IntPtr surface);
        [GlEntryPoint("eglDestroySurface")]
        public EglDisplaySurfaceVoidDelegate DestroySurface { get; }
        
        [GlEntryPoint("eglSwapBuffers")]
        public EglDisplaySurfaceVoidDelegate SwapBuffers { get; }
        
        public delegate IntPtr
            EglCreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, int[] attrs);
        [GlEntryPoint("eglCreateWindowSurface")]
        public EglCreateWindowSurface CreateWindowSurface { get; }

        public delegate bool EglGetConfigAttrib(IntPtr display, IntPtr config, int attr, out int rv);
        [GlEntryPoint("eglGetConfigAttrib")]
        public EglGetConfigAttrib GetConfigAttrib { get; }

        public delegate bool EglWaitGL();
        [GlEntryPoint("eglWaitGL")]
        public EglWaitGL WaitGL { get; }
        
        public delegate bool EglWaitClient();
        [GlEntryPoint("eglWaitClient")]
        public EglWaitGL WaitClient { get; }
        
        public delegate bool EglWaitNative();
        [GlEntryPoint("eglWaitNative")]
        public EglWaitGL WaitNative { get; }
        
        public delegate IntPtr EglQueryString(IntPtr display, int i);
        
        [GlEntryPoint("eglQueryString")]
        public EglQueryString QueryStringNative { get; }

        public string QueryString(IntPtr display, int i)
        {
            var rv = QueryStringNative(display, i);
            if (rv == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringAnsi(rv);
        }

        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
