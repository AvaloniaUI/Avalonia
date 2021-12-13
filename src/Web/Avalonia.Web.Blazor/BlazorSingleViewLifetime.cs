using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;

namespace Avalonia.Web.Blazor
{
    public class BlazorSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public Control? MainView { get; set; }
    }

    public static class WebAppBuilder
    {
        public static T SetupWithSingleViewLifetime<T>(
            this T builder)
            where T : AppBuilderBase<T>, new()
        {
            return builder.SetupWithLifetime(new BlazorSingleViewLifetime());
        }

        public static AvaloniaBlazorAppBuilder Configure<TApp>()
            where TApp : Application, new()
        {
            var builder = AvaloniaBlazorAppBuilder.Configure<TApp>()
                .UseSkia()
                .With(new SkiaOptions { CustomGpuFactory = () => new BlazorSkiaGpu() });

            return builder;
        }
    }
}
