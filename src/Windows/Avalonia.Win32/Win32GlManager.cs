using System;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.OpenGl.Angle;
using Avalonia.Win32.WinRT.Composition;

namespace Avalonia.Win32
{
    static class Win32GlManager
    {
        public static IPlatformGraphics? Initialize()
        {
            var gl = InitializeCore();

            if (gl is not null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(gl);
            }

            return gl;
        }
        
        private static IPlatformGraphics? InitializeCore()
        {
            var opts = AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions();

            var winVersion = Win32Platform.WindowsVersion;
            var renderingMode = opts.RenderingMode ??
                (winVersion > PlatformConstants.Windows7 ? Win32RenderingMode.AngleEgl : Win32RenderingMode.Software);
            
            if (renderingMode == Win32RenderingMode.Wgl)
            {
                var wgl = WglPlatformOpenGlInterface.TryCreate();
                return wgl;
            }

            if (renderingMode == Win32RenderingMode.AngleEgl)
            {
                var egl = AngleWin32PlatformGraphics.TryCreate(AvaloniaLocator.Current.GetService<AngleOptions>() ??
                                                               new());

                if (egl != null && egl.PlatformApi == AngleOptions.PlatformApi.DirectX11)
                {
                    AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphicsOpenGlContextFactory>()
                        .ToConstant(egl);

                    var compositionMode = opts.CompositionMode ??
                                          (WinUiCompositorConnection.IsSupported() ? Win32CompositionMode.WinUIComposition
                                              //: DirectCompositionConnection.IsSupported() ? Win32CompositionMode.DirectComposition
                                              : Win32CompositionMode.RedirectionSurface);

                    switch (compositionMode)
                    {
                        case Win32CompositionMode.WinUIComposition:
                            if (!WinUiCompositorConnection.TryCreateAndRegister())
                            {
                                //goto case Win32CompositionMode.DirectComposition;
                            }
                            break;
                        //case Win32CompositionMode.DirectComposition:
                            //DirectCompositionConnection.TryCreateAndRegister();
                            //break;
                        case Win32CompositionMode.LowLatencyDxgiSwapChain:
                            DxgiConnection.TryCreateAndRegister();
                            break;
                    }
                }

                return egl;
            }

            return null;
        }
    }
}
