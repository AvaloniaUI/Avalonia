using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.WinRT.Composition;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {
        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToLazy<IPlatformOpenGlInterface>(() =>
            {
                var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>();
                if (opts?.UseWgl == true)
                {
                    var wgl = WglPlatformOpenGlInterface.TryCreate();
                    return wgl;
                }

                if (opts?.AllowEglInitialization == true)
                {
                    var egl = EglPlatformOpenGlInterface.TryCreate(() => new AngleWin32EglDisplay());

                    if (egl is { } &&
                        opts?.UseWindowsUIComposition == true) 
                        WinUICompositorConnection.TryCreateAndRegister(egl);

                    return egl;
                }

                return null;
            });
        }
    }
}
