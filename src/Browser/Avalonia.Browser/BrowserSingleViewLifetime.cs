using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Runtime.Versioning;

namespace Avalonia.Browser;

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

public class BrowserPlatformOptions
{
    public Func<string, string> FrameworkAssetPathResolver { get; set; } = new(fileName => $"./{fileName}");
}

[SupportedOSPlatform("browser")]
public static class WebAppBuilder
{
    public static AppBuilder SetupBrowserApp(
        this AppBuilder builder, string mainDivId)
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

    public static AppBuilder UseBrowser(
        this AppBuilder builder)
    {
        return builder
            .UseWindowingSubsystem(BrowserWindowingPlatform.Register)
            .UseSkia();
    }
}
