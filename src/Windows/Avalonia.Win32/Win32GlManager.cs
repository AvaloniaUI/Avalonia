using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.OpenGl.Angle;
using Avalonia.Win32.WinRT.Composition;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {

        public static IPlatformGraphics Initialize()
        {
            var gl = InitializeCore();
            AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(gl);
            AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(gl);
            return gl;
        }
        
        static IPlatformGraphics InitializeCore()
        {

            var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions();
            if (opts.UseWgl)
            {
                var wgl = WglPlatformOpenGlInterface.TryCreate();
                return wgl;
            }

            if (opts.AllowEglInitialization ?? Win32Platform.WindowsVersion > PlatformConstants.Windows7)
            {
                var egl = AngleWin32PlatformGraphics.TryCreate(AvaloniaLocator.Current.GetService<AngleOptions>() ??
                                                               new());

                if (egl != null && egl.PlatformApi == AngleOptions.PlatformApi.DirectX11)
                {
                    if (opts.UseWindowsUIComposition)
                    {
                        WinUiCompositorConnection.TryCreateAndRegister();
                    }
                    else if (opts.UseLowLatencyDxgiSwapChain)
                    {
                        DxgiConnection.TryCreateAndRegister();
                    }
                }

                return egl;
            }

            return null;
        }
        
        
    }
}
