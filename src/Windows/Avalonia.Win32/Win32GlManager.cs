using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Win32.DxgiSwapchain;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.WinRT.Composition;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {

        public static IPlatformOpenGlInterface Initialize()
        {
            var gl = InitializeCore();
            AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToConstant(gl);
            AvaloniaLocator.CurrentMutable.Bind<IPlatformGpu>().ToConstant(gl);
            return gl;
        }
        
        static IPlatformOpenGlInterface InitializeCore()
        {

            var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions();
            if (opts.UseWgl)
            {
                var wgl = WglPlatformOpenGlInterface.TryCreate();
                return wgl;
            }

            if (opts.AllowEglInitialization ?? Win32Platform.WindowsVersion > PlatformConstants.Windows7)
            {
                var egl = EglPlatformOpenGlInterface.TryCreate(() => new AngleWin32EglDisplay());

                if (egl != null)
                {
                    if (opts.EglRendererBlacklist != null)
                    {
                        foreach (var item in opts.EglRendererBlacklist)
                        {
                            if (egl.PrimaryEglContext.GlInterface.Renderer.Contains(item))
                            {
                                return null;
                            }
                        }
                    }

                    if (opts.UseWindowsUIComposition)
                    {
                        WinUICompositorConnection.TryCreateAndRegister(egl, opts.CompositionBackdropCornerRadius);
                    }
                    else if (opts.UseLowLatencyDxgiSwapChain)
                    {
                        DxgiConnection.TryCreateAndRegister(egl);
                    }
                }

                return egl;
            }

            return null;
        }
        
        
    }
}
