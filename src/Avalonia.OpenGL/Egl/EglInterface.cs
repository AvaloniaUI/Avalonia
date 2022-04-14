using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL.Egl
{
    public class EglInterface : GlInterfaceBase
    {
        public EglInterface() : base(Load())
        {
            
        }

        public EglInterface(Func<Utf8Buffer,IntPtr> getProcAddress) : base(getProcAddress)
        {
            
        }
        
        public EglInterface(Func<string, IntPtr> getProcAddress) : base(getProcAddress)
        {
            
        }
        
        public EglInterface(string library) : base(Load(library))
        {
        }

        
        static Func<string, IntPtr> Load()
        {
            var os = AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem;
            if(os == OperatingSystemType.Linux)
                return Load("libEGL.so.1");
            if (os == OperatingSystemType.Android)
                return Load("libEGL.so");

            throw new PlatformNotSupportedException();
        }

        static Func<string, IntPtr> Load(string library)
        {
            var dyn = AvaloniaLocator.Current.GetService<IDynamicLibraryLoader>();
            var lib = dyn.LoadLibrary(library);
            return (s) => dyn.GetProcAddress(lib, s, true);
        }

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int EglGetError();
        [GlEntryPoint("eglGetError")]
        public EglGetError GetError { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglGetDisplay(IntPtr nativeDisplay);
        [GlEntryPoint("eglGetDisplay")]
        public EglGetDisplay GetDisplay { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglGetPlatformDisplayEXT(int platform, IntPtr nativeDisplay, int[] attrs);
        [GlEntryPoint("eglGetPlatformDisplayEXT")]
        [GlOptionalEntryPoint]
        public EglGetPlatformDisplayEXT GetPlatformDisplayEXT { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglInitialize(IntPtr display, out int major, out int minor);
        [GlEntryPoint("eglInitialize")]
        public EglInitialize Initialize { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglGetProcAddress(Utf8Buffer proc);
        [GlEntryPoint("eglGetProcAddress")]
        public EglGetProcAddress GetProcAddress { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglBindApi(int api);
        [GlEntryPoint("eglBindAPI")]
        public EglBindApi BindApi { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglChooseConfig(IntPtr display, int[] attribs,
            out IntPtr surfaceConfig, int numConfigs, out int choosenConfig);
        [GlEntryPoint("eglChooseConfig")]
        public EglChooseConfig ChooseConfig { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglCreateContext(IntPtr display, IntPtr config,
            IntPtr share, int[] attrs);
        [GlEntryPoint("eglCreateContext")]
        public EglCreateContext CreateContext { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglDestroyContext(IntPtr display, IntPtr context);
        [GlEntryPoint("eglDestroyContext")]
        public EglDestroyContext DestroyContext { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglCreatePBufferSurface(IntPtr display, IntPtr config, int[] attrs);
        [GlEntryPoint("eglCreatePbufferSurface")]
        public EglCreatePBufferSurface CreatePBufferSurface { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
        [GlEntryPoint("eglMakeCurrent")]
        public EglMakeCurrent MakeCurrent { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglGetCurrentContext();
        [GlEntryPoint("eglGetCurrentContext")]
        public EglGetCurrentContext GetCurrentContext { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglGetCurrentDisplay();
        [GlEntryPoint("eglGetCurrentDisplay")]
        public EglGetCurrentContext GetCurrentDisplay { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglGetCurrentSurface(int readDraw);
        [GlEntryPoint("eglGetCurrentSurface")] 
        public EglGetCurrentSurface GetCurrentSurface { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void EglDisplaySurfaceVoidDelegate(IntPtr display, IntPtr surface);
        [GlEntryPoint("eglDestroySurface")]
        public EglDisplaySurfaceVoidDelegate DestroySurface { get; }
        
        [GlEntryPoint("eglSwapBuffers")]
        public EglDisplaySurfaceVoidDelegate SwapBuffers { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr
            EglCreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, int[] attrs);
        [GlEntryPoint("eglCreateWindowSurface")]
        public EglCreateWindowSurface CreateWindowSurface { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglGetConfigAttrib(IntPtr display, IntPtr config, int attr, out int rv);
        [GlEntryPoint("eglGetConfigAttrib")]
        public EglGetConfigAttrib GetConfigAttrib { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglWaitGL();
        [GlEntryPoint("eglWaitGL")]
        public EglWaitGL WaitGL { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglWaitClient();
        [GlEntryPoint("eglWaitClient")]
        public EglWaitGL WaitClient { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglWaitNative(int engine);
        [GlEntryPoint("eglWaitNative")]
        public EglWaitNative WaitNative { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr EglCreatePbufferFromClientBuffer(IntPtr display, int buftype, IntPtr buffer, IntPtr config, int[] attrib_list);
        [GlEntryPoint("eglCreatePbufferFromClientBuffer")]

        public EglCreatePbufferFromClientBuffer CreatePbufferFromClientBuffer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglQueryDisplayAttribEXT(IntPtr display, int attr, out IntPtr res);

        [GlEntryPoint("eglQueryDisplayAttribEXT"), GlOptionalEntryPoint]
        public EglQueryDisplayAttribEXT QueryDisplayAttribExt { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool EglQueryDeviceAttribEXT(IntPtr display, int attr, out IntPtr res);

        [GlEntryPoint("eglQueryDeviceAttribEXT"), GlOptionalEntryPoint]
        public EglQueryDisplayAttribEXT QueryDeviceAttribExt { get; }

        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
