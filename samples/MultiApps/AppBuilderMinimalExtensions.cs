using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace Previewer
{
    public static class AppBuilderMinimalExtensions
    {
        public static TAppBuilder UseFluentTheme<TAppBuilder>(this TAppBuilder builder, FluentThemeMode mode = FluentThemeMode.Light)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        {
            return builder.AfterSetup(_ => builder.Instance.Styles.Add(new FluentTheme(new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName()}")) { Mode = mode }));
        }

        public static IDisposable StartWithClassicDesktopLifetime<T>(this T builder, Action<IClassicDesktopStyleApplicationLifetime> callback) where T : AppBuilderBase<T>, new()
        {
            var classicDesktopStyleApplicationLifetime = new ClassicDesktopStyleApplicationLifetime
            {
                ShutdownMode = ShutdownMode.OnLastWindowClose
            };

            builder.SetupWithLifetime(classicDesktopStyleApplicationLifetime);

            callback?.Invoke(classicDesktopStyleApplicationLifetime);

            classicDesktopStyleApplicationLifetime.Start(Array.Empty<string>());

            return classicDesktopStyleApplicationLifetime;
        }
    }
}
