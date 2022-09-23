using System.Runtime.InteropServices.JavaScript;
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;

namespace Avalonia.Web.Blazor
{
    public class BlazorSingleViewLifetime : ISingleViewApplicationLifetime
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
        

        public static T UseBrowserWasm<T>(
        this T builder)
        where T : AppBuilderBase<T>, new()
        {
            Console.WriteLine("In UseBrowserWasm");
            var lifetime = new BlazorSingleViewLifetime();

            return builder
                .UseWindowingSubsystem(BlazorWindowingPlatform.Register)
                .UseSkia()
                .With(new SkiaOptions { CustomGpuFactory = () => new BlazorSkiaGpu() })
                .AfterSetup(b =>
                {
                    var view = new AvaloniaView();
                    lifetime.View = view;
                    Console.WriteLine("After setup");

                })
                .SetupWithLifetime(lifetime);

        }
    }
}
