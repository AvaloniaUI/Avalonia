using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Win32.OpenGl;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {
        private static bool s_attemptedToInitialize;

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
                    return EglPlatformOpenGlInterface.TryCreate(() => new AngleWin32EglDisplay());

                return null;
            });
        }
    }
}
