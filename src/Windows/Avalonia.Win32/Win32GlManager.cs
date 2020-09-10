using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {
        /// <summary>This property is initialized if drawing platform requests OpenGL support</summary>
        public static EglGlPlatformFeature EglFeature { get; private set; }

        private static bool s_attemptedToInitialize;

        public static void Initialize()
        {
            AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatformGlFeature>().ToFunc(() =>
            {
                if (!s_attemptedToInitialize)
                {
                    EglFeature = EglGlPlatformFeature.TryCreate(() => new AngleWin32EglDisplay());
                    s_attemptedToInitialize = true;
                }

                return EglFeature;
            });
        }
    }
}
