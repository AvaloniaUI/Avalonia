// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Avalonia.Platform.Gpu
{
    public static class EGL
    {
        public const int DEFAULT_DISPLAY = 0;
        public const int CONTEXT_CLIENT_VERSION = 0x3098;
        public const int NONE = 0x3038;
        public const int SURFACE_TYPE = 0x3033;
        public const int PBUFFER_BIT = 0x0001;
        public const int WINDOW_BIT = 0x0004;
        public const int PIXMAP_BIT = 0x0002;
        public const int RED_SIZE = 0x3024;
        public const int GREEN_SIZE = 0x3023;
        public const int BLUE_SIZE = 0x3022;
        public const int ALPHA_SIZE = 0x3021;
        public const int STENCIL_SIZE = 0x3026;
        public const int RENDERABLE_TYPE = 0x3040;
        public const int OPENGL_BIT = 0x0008;
        public const int OPENGL_API = 0x30A2;
        public const int OPENGL_ES_BIT = 0x0001;
        public const int OPENGL_ES2_BIT = 0x0004;
        public const int OPENGL_ES_API = 0x30A0;
        public const int WIDTH = 0x3057;
        public const int HEIGHT = 0x3056;
        public const int NO_SURFACE = 0;
        public const int NO_DISPLAY = 0;
        public const int NO_CONTEXT = 0;
        public const int EXTENSIONS = 0x3055;

        public const int PLATFORM_ANGLE_ANGLE = 0x3202;
        public const int PLATFORM_ANGLE_TYPE_ANGLE = 0x3203;
        public const int PLATFORM_ANGLE_TYPE_DEFAULT_ANGLE = 0x3206;
        public const int PLATFORM_ANGLE_DEBUG_LAYERS_ENABLED = 0x3451;

        public const int PLATFORM_ANGLE_TYPE_D3D9_ANGLE = 0x3207;
        public const int PLATFORM_ANGLE_TYPE_D3D11_ANGLE = 0x3208;

        public const int PLATFORM_ANGLE_TYPE_OPENGL_ANGLE = 0x320D;
        public const int PLATFORM_ANGLE_TYPE_OPENGLES_ANGLE = 0x320E;

        public const int PLATFORM_ANGLE_TYPE_VULKAN_ANGLE = 0x3450;

        public const int PLATFORM_ANGLE_MAX_VERSION_MAJOR_ANGLE = 0x3204;
        public const int PLATFORM_ANGLE_MAX_VERSION_MINOR_ANGLE = 0x3205;

        private static bool s_isInitialized;

        private static class Native
        {
            public const string Library = "libEGL.dll";
            
            [DllImport(Library)]
            internal static extern IntPtr eglGetDisplay(IntPtr display_id);
            
            [DllImport(Library)]
            internal static extern bool eglInitialize(IntPtr dpy, out int major, out int minor);

            [DllImport(Library)]
            internal static extern IntPtr eglCreateContext(IntPtr dpy, IntPtr config, IntPtr share_context, int[] attrib_list);

            [DllImport(Library)]
            internal static extern bool eglChooseConfig(IntPtr dpy, int[] attrib_list, IntPtr[] configs, int config_size, out int num_config);

            [DllImport(Library)]
            internal static extern IntPtr eglCreatePbufferSurface(IntPtr dpy, IntPtr config, int[] attrib_list);

            [DllImport(Library)]
            internal static extern bool eglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);

            [DllImport(Library)]
            internal static extern IntPtr eglGetProcAddress(string funcname);

            [DllImport(Library)]
            internal static extern bool eglSwapBuffers(IntPtr dpy, IntPtr surface);

            [DllImport(Library)]
            internal static extern IntPtr eglCreateWindowSurface(IntPtr dpy, IntPtr config, IntPtr win, int[] attrib_list);

            [DllImport(Library)]
            internal static extern int eglGetError();

            [DllImport(Library)]
            internal static extern bool eglBindAPI(uint api);

            [DllImport(Library)]
            internal static extern IntPtr eglQueryString(IntPtr dpy, int name);

            [DllImport(Library)]
            internal static extern IntPtr eglGetPlatformDisplayEXT(uint platform, IntPtr native_display, int[] attrib_list);

            [DllImport(Library)]
            internal static extern bool eglDestroySurface(IntPtr dpy, IntPtr surface);
        }
        
        public static IntPtr GetDisplay(IntPtr displayId)
        {
            return Native.eglGetDisplay(displayId);
        }

        public static bool Initialize(IntPtr display, out int major, out int minor)
        {
            return Native.eglInitialize(display, out major, out minor);
        }

        public static IntPtr CreateContext(IntPtr display, IntPtr config, IntPtr shareContext, int[] attributeList)
        {
            return Native.eglCreateContext(display, config, shareContext, attributeList);
        }

        public static bool ChooseConfig(IntPtr display, int[] attributeList, IntPtr[] configs, int configsSize, out int numConfigs)
        {
            return Native.eglChooseConfig(display, attributeList, configs, configsSize, out numConfigs);
        }

        public static IntPtr CreatePbufferSurface(IntPtr display, IntPtr config, int[] attributeList)
        {
            return Native.eglCreatePbufferSurface(display, config, attributeList);
        }

        public static IntPtr CreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, int[] attributeList)
        {
            return Native.eglCreateWindowSurface(display, config, window, attributeList);
        }

        public static bool MakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context)
        {
            return Native.eglMakeCurrent(display, draw, read, context);
        }

        public static IntPtr GetProcAddress(string procName)
        {
            return Native.eglGetProcAddress(procName);
        }

        public static bool SwapBuffers(IntPtr display, IntPtr surface)
        {
            return Native.eglSwapBuffers(display, surface);
        }

        public static int GetError()
        {
            return Native.eglGetError();
        }

        public static bool BindAPI(uint api)
        {
            return Native.eglBindAPI(api);
        }

        public static string QueryString(IntPtr display, int name)
        {
            return Marshal.PtrToStringAnsi(Native.eglQueryString(display, name));
        }

        public static IntPtr GetPlatformDisplayEXT(uint platform, IntPtr nativeDisplay, int[] attributeList)
        {
            return Native.eglGetPlatformDisplayEXT(platform, nativeDisplay, attributeList);
        }

        public static bool DestroySurface(IntPtr display, IntPtr surface)
        {
            return Native.eglDestroySurface(display, surface);
        }
    }
}
