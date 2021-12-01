using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;

namespace Avalonia.Web.Blazor
{
    public class BlazorSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public Control MainView { get; set; }
    }

    public static class BlazorSingleViewLifetimeExtensions
    {


        public static AvaloniaBlazorAppBuilder SetupWithBlazorSingleViewLifetime<TApp>()
            where TApp : Application, new()
        {
            var builder = AvaloniaBlazorAppBuilder.Configure<TApp>()
            .UseSkia()
            .With(new SkiaOptions() { CustomGpuFactory = () => new BlazorSkiaGpu() })
            .SetupWithLifetime(new BlazorSingleViewLifetime());

            AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant(new FontManager(new CustomFontManagerImpl()));

            return builder;
        }
    }
}
