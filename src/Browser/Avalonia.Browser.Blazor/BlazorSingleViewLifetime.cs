using System.Runtime.Versioning;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Browser.Blazor;

[SupportedOSPlatform("browser")]
public static class WebAppBuilder
{
    public static T SetupWithSingleViewLifetime<T>(
        this T builder)
        where T : AppBuilderBase<T>, new()
    {
        return builder.SetupWithLifetime(new BlazorSingleViewLifetime());
    }

    public static T UseBlazor<T>(this T builder) where T : AppBuilderBase<T>, new()
    {
        return builder
            .UseBrowser()
            .With(new BrowserPlatformOptions
            {
                FrameworkAssetPathResolver = new(filePath => $"/_content/Avalonia.Browser.Blazor/{filePath}")
            });
    }

    public static AppBuilder Configure<TApp>()
        where TApp : Application, new()
    {
        return AppBuilder.Configure<TApp>()
            .UseBlazor();
    }

    internal class BlazorSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public Control? MainView { get; set; }
    }
}
