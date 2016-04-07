using System;
using System.Reactive.Disposables;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Controls.Platform
{
    public static partial class PlatformManager
    {
        static IPlatformSettings GetSettings()
            => PerspexLocator.Current.GetService<IPlatformSettings>();

        static bool s_designerMode;
        private static double _designerScalingFactor = 1;

        public static IRenderTarget CreateRenderTarget(ITopLevelImpl window)
        {
            return PerspexLocator.Current
                .GetService<IPlatformRenderInterface>()
                .CreateRenderer(window.Handle);
        }

        public static IDisposable DesignerMode()
        {
            s_designerMode = true;
            return Disposable.Create(() => s_designerMode = false);
        }

        public static void SetDesignerScalingFactor(double factor)
        {
            _designerScalingFactor = factor;
        }

        public static IWindowImpl CreateWindow()
        {
            var platform = PerspexLocator.Current.GetService<IWindowingPlatform>();
            
            if (platform == null)
            {
                throw new Exception("Could not CreateWindow(): IWindowingPlatform is not registered.");
            }

            return s_designerMode ? platform.CreateEmbeddableWindow() : platform.CreateWindow();
        }

        public static IPopupImpl CreatePopup()
        {
            return PerspexLocator.Current.GetService<IWindowingPlatform>().CreatePopup();
        }
    }
}
