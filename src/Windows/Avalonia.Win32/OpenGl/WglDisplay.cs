using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using static Avalonia.Win32.OpenGl.WglConsts;
namespace Avalonia.Win32.OpenGl
{
    internal class WglDisplay
    {
        private static bool? _initialized;
        private static readonly DebugCallbackDelegate _debugCallback = DebugCallback;

        private static IntPtr _bootstrapContext;
        private static IntPtr _bootstrapWindow;
        private static IntPtr _bootstrapDc;
        private static PixelFormatDescriptor _defaultPfd;
        private static int _defaultPixelFormat;
        public static IntPtr OpenGl32Handle = LoadLibrary("opengl32");

        private delegate bool WglChoosePixelFormatARBDelegate(IntPtr hdc, int[] piAttribIList, float[] pfAttribFList,
            int nMaxFormats, int[] piFormats, out int nNumFormats);

        private static WglChoosePixelFormatARBDelegate WglChoosePixelFormatArb;

        private delegate IntPtr WglCreateContextAttribsARBDelegate(IntPtr hDC, IntPtr hShareContext, int[] attribList);

        private static WglCreateContextAttribsARBDelegate WglCreateContextAttribsArb;
        
        private delegate void GlDebugMessageCallbackDelegate(IntPtr callback, IntPtr userParam);

        private static GlDebugMessageCallbackDelegate GlDebugMessageCallback;

        private delegate void DebugCallbackDelegate(int source, int type, int id, int severity, int len, IntPtr message,
            IntPtr userParam);
        
        static bool Initialize()
        {
            if (!_initialized.HasValue)
                _initialized = InitializeCore();
            return _initialized.Value;
        }
        static bool InitializeCore()
        {
            Dispatcher.UIThread.VerifyAccess();
            _bootstrapWindow = WglGdiResourceManager.CreateOffscreenWindow();
            _bootstrapDc = WglGdiResourceManager.GetDC(_bootstrapWindow);
            _defaultPfd = new PixelFormatDescriptor
            {
                Size = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
                Version = 1,
                Flags = PixelFormatDescriptorFlags.PFD_DRAW_TO_WINDOW |
                        PixelFormatDescriptorFlags.PFD_SUPPORT_OPENGL | PixelFormatDescriptorFlags.PFD_DOUBLEBUFFER,
                DepthBits = 24,
                StencilBits = 8,
                ColorBits = 32
            };
            _defaultPixelFormat = ChoosePixelFormat(_bootstrapDc, ref _defaultPfd);
            SetPixelFormat(_bootstrapDc, _defaultPixelFormat, ref _defaultPfd);

            _bootstrapContext = wglCreateContext(_bootstrapDc);
            if (_bootstrapContext == IntPtr.Zero)
                return false;

            wglMakeCurrent(_bootstrapDc, _bootstrapContext);
            WglCreateContextAttribsArb = Marshal.GetDelegateForFunctionPointer<WglCreateContextAttribsARBDelegate>(
                wglGetProcAddress("wglCreateContextAttribsARB"));

            WglChoosePixelFormatArb =
                Marshal.GetDelegateForFunctionPointer<WglChoosePixelFormatARBDelegate>(
                    wglGetProcAddress("wglChoosePixelFormatARB"));

            GlDebugMessageCallback =
                Marshal.GetDelegateForFunctionPointer<GlDebugMessageCallbackDelegate>(
                    wglGetProcAddress("glDebugMessageCallback"));
            

            var formats = new int[1];
            WglChoosePixelFormatArb(_bootstrapDc, new int[]
            {
                WGL_DRAW_TO_WINDOW_ARB, 1,
                WGL_ACCELERATION_ARB, WGL_FULL_ACCELERATION_ARB,
                WGL_SUPPORT_OPENGL_ARB, 1,
                WGL_DOUBLE_BUFFER_ARB, 1,
                WGL_PIXEL_TYPE_ARB, WGL_TYPE_RGBA_ARB,
                WGL_COLOR_BITS_ARB, 32,
                WGL_ALPHA_BITS_ARB, 8,
                WGL_DEPTH_BITS_ARB, 0,
                WGL_STENCIL_BITS_ARB, 0,
                0, // End
            }, null, 1, formats, out int numFormats);
            if (numFormats != 0)
            {
                DescribePixelFormat(_bootstrapDc, formats[0], Marshal.SizeOf<PixelFormatDescriptor>(), ref _defaultPfd);
                _defaultPixelFormat = formats[0];
            }
            
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        private static void DebugCallback(int source, int type, int id, int severity, int len, IntPtr message, IntPtr userparam)
        {
            var err = Marshal.PtrToStringAnsi(message, len);
            Console.Error.WriteLine(err);
        }
        
        public static WglContext CreateContext(GlVersion[] versions, IGlContext share)
        {
            if (!Initialize())
                return null;

            var shareContext = (WglContext)share;

            using (new WglRestoreContext(_bootstrapDc, _bootstrapContext, null))
            {
                var window = WglGdiResourceManager.CreateOffscreenWindow();
                var dc = WglGdiResourceManager.GetDC(window);
                SetPixelFormat(dc, _defaultPixelFormat, ref _defaultPfd);
                foreach (var version in versions)
                {
                    if(version.Type != GlProfileType.OpenGL)
                        continue;
                    IntPtr context;
                    using (shareContext?.Lock())
                    {
                        context = WglCreateContextAttribsArb(dc, shareContext?.Handle ?? IntPtr.Zero,
                            new[]
                            {
                                // major
                                WGL_CONTEXT_MAJOR_VERSION_ARB, version.Major,
                                // minor
                                WGL_CONTEXT_MINOR_VERSION_ARB, version.Minor,
                                // core profile
                                WGL_CONTEXT_PROFILE_MASK_ARB, 1,
                                // debug 
                                // WGL_CONTEXT_FLAGS_ARB, 1,
                                // end
                                0, 0
                            });
                    }

                    using(new WglRestoreContext(dc, context, null))
                        GlDebugMessageCallback(Marshal.GetFunctionPointerForDelegate(_debugCallback), IntPtr.Zero);
                    if (context != IntPtr.Zero)
                        return new WglContext(shareContext, version, context, window, dc,
                            _defaultPixelFormat, _defaultPfd);
                }

                WglGdiResourceManager.ReleaseDC(window, dc);
                WglGdiResourceManager.DestroyWindow(window);
                return null;
            }
        }



    }
}
