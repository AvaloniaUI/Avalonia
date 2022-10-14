using Avalonia;
using Avalonia.Maui;
using Avalonia.Maui.Controls;
using Avalonia.Maui.Handlers;

namespace MauiSample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseAvalonia<ControlCatalog.App>(appBuilder =>
                {
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {

#if ANDROID
                    handlers.AddHandler(typeof(AvaloniaView), typeof(AvaloniaViewHandler));
#endif
                });

            return builder.Build();
        }
    }
}