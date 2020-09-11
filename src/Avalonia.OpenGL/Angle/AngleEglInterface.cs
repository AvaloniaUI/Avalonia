using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL.Angle
{
    public class AngleEglInterface : EglInterface
    {
        [DllImport("libglesv2.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr EGL_GetProcAddress(string proc);

        public AngleEglInterface() : base(LoadAngle())
        {

        }

        static Func<string, IntPtr> LoadAngle()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();
            {
                var disp = EGL_GetProcAddress("eglGetPlatformDisplayEXT");
                if (disp == IntPtr.Zero)
                    throw new OpenGlException("libegl.dll doesn't have eglGetPlatformDisplayEXT entry point");
                return EGL_GetProcAddress;
            }
        }

    }
}
