using System;
using System.Linq;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.Win32.OpenGl
{
    internal class WglPlatformOpenGlInterface : IPlatformGraphics
    {
        public WglContext PrimaryContext { get; }
        public bool UsesSharedContext => false;
        IPlatformGraphicsContext IPlatformGraphics.CreateContext() => CreateContext();
        public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();

        public IGlContext CreateContext()
            => WglDisplay.CreateContext(new[] { PrimaryContext.Version }, null)
               ?? throw new OpenGlException("Unable to create additional WGL context");

        private WglPlatformOpenGlInterface(WglContext primary)
        {
            PrimaryContext = primary;
        }

        public static WglPlatformOpenGlInterface? TryCreate()
        {
            try
            {
                var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions();
                if (WglDisplay.CreateContext(opts.WglProfiles.ToArray(), null) is { } primary)
                {
                    return new WglPlatformOpenGlInterface(primary);
                }
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("WGL", "Unable to initialize WGL: " + e);
            }

            return null;
        }
    }
}
