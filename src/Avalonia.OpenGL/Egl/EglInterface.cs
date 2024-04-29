using System;
using System.Runtime.InteropServices;
using Avalonia.Compatibility;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using Avalonia.SourceGenerator;

namespace Avalonia.OpenGL.Egl
{
    public unsafe partial class EglInterface
    {
        public EglInterface(Func<string, IntPtr> getProcAddress)
        {
            Initialize(getProcAddress);
        }
        
        public EglInterface(string library) : this(Load(library))
        {
        }

        public EglInterface() : this(Load())
        {
            
        }

        static Func<string, IntPtr> Load()
        {
            if(OperatingSystemEx.IsLinux())
                return Load("libEGL.so.1");
            if (OperatingSystemEx.IsAndroid())
                return Load("libEGL.so");

            throw new PlatformNotSupportedException();
        }

        static Func<string, IntPtr> Load(string library)
        {
            var lib = NativeLibraryEx.Load(library);
            return (s) => NativeLibraryEx.TryGetExport(lib, s, out var address) ? address : default;
        }

        // ReSharper disable UnassignedGetOnlyAutoProperty
        
        [GetProcAddress("eglGetError")]
        public partial int GetError();
        
        [GetProcAddress("eglGetDisplay")]
        public partial IntPtr GetDisplay(IntPtr nativeDisplay);
        
        [GetProcAddress("eglGetPlatformDisplayEXT", true)]
        public partial IntPtr GetPlatformDisplayExt(int platform, IntPtr nativeDisplay, int[]? attrs);

        [GetProcAddress("eglInitialize")]        
        public partial bool Initialize(IntPtr display, out int major, out int minor);
        
        [GetProcAddress("eglTerminate")]        
        public partial void Terminate(IntPtr display);

        [GetProcAddress("eglGetProcAddress")]        
        public partial IntPtr GetProcAddress(IntPtr proc);

        [GetProcAddress("eglBindAPI")]
        public partial bool BindApi(int api);

        [GetProcAddress("eglChooseConfig")]
        public partial bool ChooseConfig(IntPtr display, int[] attribs,
            out IntPtr surfaceConfig, int numConfigs, out int choosenConfig);
        
        [GetProcAddress("eglCreateContext")]
        public partial IntPtr CreateContext(IntPtr display, IntPtr config,
            IntPtr share, int[] attrs);
        
        [GetProcAddress("eglDestroyContext")]
        public partial bool DestroyContext(IntPtr display, IntPtr context);
        
        [GetProcAddress("eglCreatePbufferSurface")]
        public partial IntPtr CreatePBufferSurface(IntPtr display, IntPtr config, int[]? attrs);

        [GetProcAddress("eglMakeCurrent")]
        public partial bool MakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
        
        [GetProcAddress("eglGetCurrentContext")]
        public partial IntPtr GetCurrentContext();

        [GetProcAddress("eglGetCurrentDisplay")]
        public partial IntPtr GetCurrentDisplay();
        
        [GetProcAddress("eglGetCurrentSurface")] 
        public partial IntPtr GetCurrentSurface(int readDraw);

        [GetProcAddress("eglDestroySurface")]
        public partial void DestroySurface(IntPtr display, IntPtr surface);

        [GetProcAddress("eglSwapBuffers")]
        public partial void SwapBuffers(IntPtr display, IntPtr surface);

        [GetProcAddress("eglCreateWindowSurface")]
        public partial IntPtr CreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, int[]? attrs);

        [GetProcAddress("eglBindTexImage")]
        public partial int BindTexImage(IntPtr display, IntPtr surface, int buffer);

        [GetProcAddress("eglGetConfigAttrib")]
        public partial bool GetConfigAttrib(IntPtr display, IntPtr config, int attr, out int rv);
        
        [GetProcAddress("eglWaitGL")]
        public partial bool WaitGL();
        
        [GetProcAddress("eglWaitClient")]
        public partial bool WaitClient();
        
        [GetProcAddress("eglWaitNative")]
        public partial bool WaitNative(int engine);
        
        [GetProcAddress("eglQueryString")]
        public partial IntPtr QueryStringNative(IntPtr display, int i);
        
        public string? QueryString(IntPtr display, int i)
        {
            var rv = QueryStringNative(display, i);
            if (rv == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringAnsi(rv);
        }

        [GetProcAddress("eglCreatePbufferFromClientBuffer")]
        public partial IntPtr CreatePbufferFromClientBuffer(IntPtr display, int buftype, IntPtr buffer, IntPtr config, int[]? attrib_list);

        [GetProcAddress("eglCreatePbufferFromClientBuffer")]
        public partial IntPtr CreatePbufferFromClientBufferPtr(IntPtr display, int buftype, IntPtr buffer, IntPtr config, int* attrib_list);

        [GetProcAddress("eglQueryDisplayAttribEXT", true)]
        public partial bool QueryDisplayAttribExt(IntPtr display, int attr, out IntPtr res);

        
        [GetProcAddress("eglQueryDeviceAttribEXT", true)]
        public partial bool QueryDeviceAttribExt(IntPtr display, int attr, out IntPtr res);
    }
}
