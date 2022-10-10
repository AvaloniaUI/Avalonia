using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Web.Blazor;

public static class WebAppBuilder
{
    public static T SetupWithSingleViewLifetime<T>(
        this T builder)
        where T : AppBuilderBase<T>, new()
    {
        return builder.SetupWithLifetime(new BlazorSingleViewLifetime());
    }

    public static AppBuilder Configure<TApp>()
        where TApp : Application, new()
    {
        var builder = AppBuilder.Configure<TApp>()
            .UseBrowser();

        return builder;
    }

    internal class BlazorSingleViewLifetime : ISingleViewApplicationLifetime
    {
        public Control? MainView { get; set; }
    }
}
