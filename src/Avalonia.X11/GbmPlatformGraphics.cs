using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;

namespace Avalonia.X11;

class GbmPlatformGraphics
{
    [DllImport("libEGL.so.1")]
    static extern IntPtr eglGetProcAddress(string proc);
    
    [DllImport("libgbm.so.1", SetLastError = true)]
    public static extern IntPtr gbm_create_device(int fd);
    
    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    public static extern int open(string pathname, int flags, int mode);
    
    public static IPlatformGraphics? TryCreate(string path)
    {
        var fd = open(path, 2, 0);
        if (fd == -1)
            return null;
        var gbmDevice = gbm_create_device(fd);
        if (gbmDevice == IntPtr.Zero)
            return null;
        return EglPlatformGraphics.TryCreate(() => new EglDisplay(new EglDisplayCreationOptions
        {
            Egl = new EglInterface(eglGetProcAddress),
            PlatformDisplay = gbmDevice,
            PlatformType = 0x31D7,
            SupportsMultipleContexts = true,
            SupportsContextSharing = true
        }));
    }
}