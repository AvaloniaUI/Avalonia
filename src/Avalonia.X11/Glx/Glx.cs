using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.X11.Glx
{
    static unsafe class Glx
    {
        private const string libGL = "libGL.so.1";
        [DllImport(libGL, EntryPoint = "glXMakeCurrent")]
        public static extern bool GlxMakeCurrent(IntPtr display, IntPtr drawable, IntPtr context);

        
        [DllImport(libGL, EntryPoint = "glXChooseVisual")]
        public static extern  XVisualInfo* GlxChooseVisual(IntPtr dpy, int screen, int[] attribList);
        
        [DllImport(libGL, EntryPoint = "glXCreateContext")]
        public static extern  IntPtr GlxCreateContext(IntPtr dpy,  XVisualInfo* vis,  IntPtr shareList,  bool direct);
        

        [DllImport(libGL, EntryPoint = "glXGetProcAddress")]
        public static extern  IntPtr GlxGetProcAddress(Utf8Buffer buffer);
        
        [DllImport(libGL, EntryPoint = "glXDestroyContext")]
        public static extern  void GlxDestroyContext(IntPtr dpy, IntPtr ctx);
        
        
        [DllImport(libGL, EntryPoint = "glXChooseFBConfig")]
        public static extern  IntPtr* GlxChooseFBConfig(IntPtr dpy,  int screen,  int[] attrib_list,  out int nelements);
        
        public static IntPtr* GlxChooseFbConfig(IntPtr dpy, int screen, IEnumerable<int> attribs, out int nelements)
        {
            var arr = attribs.Concat(new[]{0}).ToArray();
            return GlxChooseFBConfig(dpy, screen, arr, out nelements);
        }
        [DllImport(libGL, EntryPoint = "glXGetVisualFromFBConfig")]
        public static extern  XVisualInfo * GlxGetVisualFromFBConfig(IntPtr dpy,  IntPtr config);
        
        
        [DllImport(libGL, EntryPoint = "glXGetFBConfigAttrib")]
        public static extern  int GlxGetFBConfigAttrib(IntPtr dpy, IntPtr config, int attribute, out int value);
        
        
        [DllImport(libGL, EntryPoint = "glXSwapBuffers")]
        public static extern  void GlxSwapBuffers(IntPtr dpy,  IntPtr drawable);
        
        [DllImport(libGL, EntryPoint = "glXWaitX")]
        public static extern  void GlxWaitX();
        
        [DllImport(libGL, EntryPoint = "glXWaitGL")]
        public static extern  void GlxWaitGL();
    }
}
