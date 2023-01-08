using System;
using Avalonia.Reactive;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    [Unstable]
    public static partial class PlatformManager
    {
        static bool s_designerMode;

        public static IDisposable DesignerMode()
        {
            s_designerMode = true;
            return Disposable.Create(() => s_designerMode = false);
        }

        public static void SetDesignerScalingFactor(double factor)
        {
        }

        public static ITrayIconImpl? CreateTrayIcon() =>
            s_designerMode ? null : AvaloniaLocator.Current.GetService<IWindowingPlatform>()?.CreateTrayIcon();


        public static IWindowImpl CreateWindow()
        {
            var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();

            return s_designerMode ? platform.CreateEmbeddableWindow() : platform.CreateWindow();
        }

        public static IWindowImpl CreateEmbeddableWindow()
        {
            var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();
            return platform.CreateEmbeddableWindow();
        }
    }
}
