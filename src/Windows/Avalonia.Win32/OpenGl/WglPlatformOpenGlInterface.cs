using System;
using System.Linq;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.Win32.OpenGl
{
    class WglPlatformOpenGlInterface : IPlatformOpenGlInterface
    {
        public WglContext PrimaryContext { get; }
        IPlatformGpuContext IPlatformGpu.PrimaryContext => PrimaryContext;
        IGlContext IPlatformOpenGlInterface.PrimaryContext => PrimaryContext;
        public IGlContext CreateSharedContext() => WglDisplay.CreateContext(new[] { PrimaryContext.Version }, PrimaryContext);

        public bool CanShareContexts => true;
        public bool CanCreateContexts => true;
        public IGlContext CreateContext() => WglDisplay.CreateContext(new[] { PrimaryContext.Version }, null);

        private  WglPlatformOpenGlInterface(WglContext primary)
        {
            PrimaryContext = primary;
        }

        public static WglPlatformOpenGlInterface TryCreate()
        {
            try
            {
                var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions();
                var primary = WglDisplay.CreateContext(opts.WglProfiles.ToArray(), null);
                return new WglPlatformOpenGlInterface(primary);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("WGL", "Unable to initialize WGL: " + e);
            }

            return null;
        }
    }
}
