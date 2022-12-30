using System.Runtime.Versioning;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Browser.Blazor;

[SupportedOSPlatform("browser")]
public static class WebAppBuilder
{
    public static AppBuilder SetupWithSingleViewLifetime(
        this AppBuilder builder)
    {
        return builder.SetupWithLifetime(new BlazorSingleViewLifetime());
    }

    public static AppBuilder UseBlazor(this AppBuilder builder)
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
