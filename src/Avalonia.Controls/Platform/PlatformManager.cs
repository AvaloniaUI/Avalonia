using System;
using System.Reactive.Disposables;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    public static partial class PlatformManager
    {
        static IPlatformSettings GetSettings()
            => AvaloniaLocator.Current.GetService<IPlatformSettings>();

        static bool s_designerMode;

        public static IDisposable DesignerMode()
        {
            s_designerMode = true;
            return Disposable.Create(() => s_designerMode = false);
        }

        public static void SetDesignerScalingFactor(double factor)
        {
        }

        public static IWindowImpl CreateWindow()
        {
            var platform = AvaloniaLocator.Current.GetService<IWindowingPlatform>();
            
            if (platform == null)
            {
                throw new Exception("Could not CreateWindow(): IWindowingPlatform is not registered.");
            }

            return s_designerMode ? (IWindowImpl)platform.CreateEmbeddableWindow() : platform.CreateWindow();
        }

        public static IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            var platform = AvaloniaLocator.Current.GetService<IWindowingPlatform>();
            if (platform == null)
                throw new Exception("Could not CreateEmbeddableWindow(): IWindowingPlatform is not registered.");
            return platform.CreateEmbeddableWindow();
        }
    }
}
