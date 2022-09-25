using System.Runtime.InteropServices.JavaScript;
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;

namespace Avalonia.Web
{
    public class BrowserSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public AvaloniaView? View;

        public Control? MainView
        {
            get => View!.Content;
            set => View!.Content = value;
        }
    }

    public static partial class WebAppBuilder
    {
        public static T SetupBrowserApp<T>(
        this T builder, string mainDivId)
        where T : AppBuilderBase<T>, new()
        {
            var lifetime = new BrowserSingleViewLifetime();

            return builder
                .UseWindowingSubsystem(BrowserWindowingPlatform.Register)
                .UseSkia()
                .With(new SkiaOptions { CustomGpuFactory = () => new BrowserSkiaGpu() })
                .AfterSetup(b =>
                {
                    lifetime.View = new AvaloniaView(mainDivId);
                })
                .SetupWithLifetime(lifetime);
        }
    }
}
