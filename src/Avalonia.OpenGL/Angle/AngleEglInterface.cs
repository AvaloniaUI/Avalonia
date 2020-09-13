using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL.Angle
{
    public class AngleEglInterface : EglInterface
    {
        [DllImport("libegl.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr eglGetProcAddress(string proc);

        public AngleEglInterface() : base(LoadAngle())
        {

        }

        static Func<string, IntPtr> LoadAngle()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var disp = eglGetProcAddress("eglGetPlatformDisplayEXT");

                if (disp == IntPtr.Zero)
                {
                    throw new OpenGlException("libegl.dll doesn't have eglGetPlatformDisplayEXT entry point");
                }

                return eglGetProcAddress;
            }
            
            throw new PlatformNotSupportedException();
        }

    }
}
