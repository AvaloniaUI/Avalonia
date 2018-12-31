using System;
using Avalonia.Logging;
using Avalonia.OpenGL;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {
        /// <summary>This property is initialized if drawing platform requests OpenGL support</summary>
        public static EglGlPlatformFeature EglFeature { get; private set; }

        private static bool s_attemptedToInitialize;

        public static void Initialize(bool throwIfUnavailable = false)
        {
            AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatformGlFeature>().ToFunc(() =>
            {
                if (!s_attemptedToInitialize)
                {
                    try
                    {
                        EglFeature = EglGlPlatformFeature.Create();
                    }
                    catch (Exception e)
                    {
                        if (throwIfUnavailable)
                        {
                            throw;
                        }

                        Logger.Error("OpenGL", null, "Unable to initialize EGL-based rendering: {0}", e);
                    }
                    
                    s_attemptedToInitialize = true;
                }
                else if (throwIfUnavailable && EglFeature == null)
                {
                    throw new InvalidOperationException("Unable to initialize EGL-based rendering.");
                }

                return EglFeature;
            });
        }
    }
}
