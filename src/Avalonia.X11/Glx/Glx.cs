using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.Platform.Interop;
using Avalonia.SourceGenerator;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Avalonia.X11.Glx
{
    internal unsafe partial class GlxInterface
    {
        private const string libGL = "libGL.so.1";
        [GetProcAddress("glXMakeContextCurrent")]
        public partial bool MakeContextCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);

        [GetProcAddress("glXGetCurrentContext")]
        public partial IntPtr GetCurrentContext();

        [GetProcAddress("glXGetCurrentDisplay")]
        public partial IntPtr GetCurrentDisplay();
        
        [GetProcAddress("glXGetCurrentDrawable")]
        public partial IntPtr GetCurrentDrawable();
        
        [GetProcAddress("glXGetCurrentReadDrawable")]
        public partial IntPtr GetCurrentReadDrawable();
        
        [GetProcAddress("glXCreatePbuffer")]
        public partial IntPtr CreatePbuffer(IntPtr dpy, IntPtr fbc, int[] attrib_list);
        
        [GetProcAddress("glXDestroyPbuffer")]
        public partial IntPtr DestroyPbuffer(IntPtr dpy, IntPtr fb);
        
        [GetProcAddress("glXChooseVisual")]
        public partial  XVisualInfo* ChooseVisual(IntPtr dpy, int screen, int[] attribList);
        
        
        [GetProcAddress("glXCreateContext")]
        public partial  IntPtr CreateContext(IntPtr dpy,  XVisualInfo* vis,  IntPtr shareList,  bool direct);
        

        [GetProcAddress("glXCreateContextAttribsARB")]
        public partial IntPtr CreateContextAttribsARB(IntPtr dpy, IntPtr fbconfig, IntPtr shareList,
            bool direct, int[] attribs);
        

        [DllImport(libGL, EntryPoint = "glXGetProcAddress")]
        public static extern IntPtr GlxGetProcAddress(string buffer);

        
        [GetProcAddress("glXDestroyContext")]
        public partial  void DestroyContext(IntPtr dpy, IntPtr ctx);
        
        
        [GetProcAddress("glXChooseFBConfig")]
        public partial  IntPtr* ChooseFBConfig(IntPtr dpy,  int screen,  int[] attrib_list,  out int nelements);
        
        
        public  IntPtr* ChooseFbConfig(IntPtr dpy, int screen, IEnumerable<int> attribs, out int nelements)
        {
            var arr = attribs.Concat(new[]{0}).ToArray();
            return ChooseFBConfig(dpy, screen, arr, out nelements);
        }
        
        [GetProcAddress("glXGetVisualFromFBConfig")]
        public partial  XVisualInfo * GetVisualFromFBConfig(IntPtr dpy,  IntPtr config);
        
        
        [GetProcAddress("glXGetFBConfigAttrib")]
        public partial  int GetFBConfigAttrib(IntPtr dpy, IntPtr config, int attribute, out int value);
        
        
        [GetProcAddress("glXSwapBuffers")]
        public partial  void SwapBuffers(IntPtr dpy,  IntPtr drawable);
        
        
        [GetProcAddress("glXWaitX")]
        public partial  void WaitX();
        
        
        [GetProcAddress("glXWaitGL")]
        public partial void WaitGL();
        

        [GetProcAddress("glGetError")]
        public partial int GlGetError();

        
        [GetProcAddress("glXQueryExtensionsString")]
        public partial IntPtr QueryExtensionsString(IntPtr display, int screen);

        public GlxInterface()
        {
            Initialize(SafeGetProcAddress);
        }

        // Ignores egl functions.
        // On some Linux systems, glXGetProcAddress will return valid pointers for even EGL functions.
        // This makes Skia try to load some data from EGL,
        // which can then cause segmentation faults because they return garbage.
        public static IntPtr SafeGetProcAddress(string proc)
        {
            if (proc.StartsWith("egl", StringComparison.InvariantCulture))
            {
                return IntPtr.Zero;
            }

            return GlxGetProcAddress(proc);
        }


        public string[] GetExtensions(IntPtr display)
        {
            var s = Marshal.PtrToStringAnsi(QueryExtensionsString(display, 0));
            return s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToArray();

        }
    }
}
