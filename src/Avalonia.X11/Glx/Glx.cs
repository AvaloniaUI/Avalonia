using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.Platform.Interop;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Avalonia.X11.Glx
{
    unsafe class GlxInterface : GlInterfaceBase
    {
        private const string libGL = "libGL.so.1";
        [GlEntryPointAttribute("glXMakeContextCurrent")]
        public GlxMakeContextCurrent MakeContextCurrent { get; }
        public delegate bool GlxMakeContextCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);

        [GlEntryPoint("glXCreatePbuffer")]
        public GlxCreatePbuffer CreatePbuffer { get; }

        public delegate IntPtr GlxCreatePbuffer(IntPtr dpy, IntPtr fbc, int[] attrib_list);
        
        [GlEntryPointAttribute("glXChooseVisual")]
        public GlxChooseVisual ChooseVisual { get; }
        public delegate  XVisualInfo* GlxChooseVisual(IntPtr dpy, int screen, int[] attribList);
        
        
        [GlEntryPointAttribute("glXCreateContext")]
        public GlxCreateContext CreateContext { get; }
        public delegate  IntPtr GlxCreateContext(IntPtr dpy,  XVisualInfo* vis,  IntPtr shareList,  bool direct);
        

        [GlEntryPointAttribute("glXCreateContextAttribsARB")]
        public GlxCreateContextAttribsARB CreateContextAttribsARB { get; }
        public delegate IntPtr GlxCreateContextAttribsARB(IntPtr dpy, IntPtr fbconfig, IntPtr shareList,
            bool direct, int[] attribs);
        

        [DllImport(libGL, EntryPoint = "glXGetProcAddress")]
        public static extern IntPtr GlxGetProcAddress(Utf8Buffer buffer);

        
        [GlEntryPointAttribute("glXDestroyContext")]
        public GlxDestroyContext DestroyContext { get; }
        public delegate  void GlxDestroyContext(IntPtr dpy, IntPtr ctx);
        
        
        [GlEntryPointAttribute("glXChooseFBConfig")]
        public GlxChooseFBConfig ChooseFBConfig { get; }
        public delegate  IntPtr* GlxChooseFBConfig(IntPtr dpy,  int screen,  int[] attrib_list,  out int nelements);
        
        
        public  IntPtr* GlxChooseFbConfig(IntPtr dpy, int screen, IEnumerable<int> attribs, out int nelements)
        {
            var arr = attribs.Concat(new[]{0}).ToArray();
            return ChooseFBConfig(dpy, screen, arr, out nelements);
        }
        
        [GlEntryPointAttribute("glXGetVisualFromFBConfig")]
        public GlxGetVisualFromFBConfig GetVisualFromFBConfig { get; }
        public delegate  XVisualInfo * GlxGetVisualFromFBConfig(IntPtr dpy,  IntPtr config);
        
        
        [GlEntryPointAttribute("glXGetFBConfigAttrib")]
        public GlxGetFBConfigAttrib GetFBConfigAttrib { get; }
        public delegate  int GlxGetFBConfigAttrib(IntPtr dpy, IntPtr config, int attribute, out int value);
        
        
        [GlEntryPointAttribute("glXSwapBuffers")]
        public GlxSwapBuffers SwapBuffers { get; }
        public delegate  void GlxSwapBuffers(IntPtr dpy,  IntPtr drawable);
        
        
        [GlEntryPointAttribute("glXWaitX")]
        public GlxWaitX WaitX { get; }
        public delegate  void GlxWaitX();
        
        
        [GlEntryPointAttribute("glXWaitGL")]
        public GlxWaitGL WaitGL { get; }
        public delegate  void GlxWaitGL();
        
        public delegate int GlGetError();
        [GlEntryPoint("glGetError")]
        public GlGetError GetError { get; }

        public GlxInterface() : base(GlxGetProcAddress)
        {
        }
    }
}
