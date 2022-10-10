using System.Runtime.InteropServices.JavaScript;
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Web.Skia;
using System.Runtime.Versioning;

namespace Avalonia.Web
{
    [SupportedOSPlatform("browser")]
    public class BrowserSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public AvaloniaView? View;

        public Control? MainView
        {
            get => View!.Content;
            set => View!.Content = value;
        }
    }

    [SupportedOSPlatform("browser")]
    public static partial class WebAppBuilder
    {
        public static T SetupBrowserApp<T>(
            this T builder, string mainDivId)
            where T : AppBuilderBase<T>, new()
        {
            var lifetime = new BrowserSingleViewLifetime();

            return builder
                .UseBrowser()
                .AfterSetup(b =>
                {
                    lifetime.View = new AvaloniaView(mainDivId);
                })
                .SetupWithLifetime(lifetime);
        }

        public static T UseBrowser<T>(
            this T builder)
            where T : AppBuilderBase<T>, new()
        {
            return builder
                .UseWindowingSubsystem(BrowserWindowingPlatform.Register)
                .UseSkia()
                .With(new SkiaOptions { CustomGpuFactory = () => new BrowserSkiaGpu() });
        }
    }
}
