using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {
        /// <summary>This property is initialized if drawing platform requests OpenGL support</summary>
        public static EglPlatformOpenGlInterface EglPlatformInterface { get; private set; }

        private static bool s_attemptedToInitialize;

        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToFunc(() =>
            {
                if (!s_attemptedToInitialize)
                {
                    EglPlatformInterface = EglPlatformOpenGlInterface.TryCreate(() => new AngleWin32EglDisplay());
                    s_attemptedToInitialize = true;
                }

                return EglPlatformInterface;
            });
        }
    }
}
