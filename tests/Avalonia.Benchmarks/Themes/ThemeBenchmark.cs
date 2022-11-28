using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.UnitTests;

using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Themes
{
    [MemoryDiagnoser]
    public class ThemeBenchmark : IDisposable
    {
        private IDisposable _app;

        public ThemeBenchmark()
        {
            AssetLoader.RegisterResUriParsers();

            _app = UnitTestApplication.Start(TestServices.StyledWindow.With(theme: () => null));
            // Add empty style to override it later
            UnitTestApplication.Current.Styles.Add(new Style());
        }

        [Benchmark]
        [Arguments(FluentThemeMode.Dark)]
        [Arguments(FluentThemeMode.Light)]
        public bool InitFluentTheme(FluentThemeMode mode)
        {
            UnitTestApplication.Current.Styles[0] = new FluentTheme()
            {
                Mode = mode
            };
            return ((IResourceHost)UnitTestApplication.Current).TryGetResource("SystemAccentColor", out _);
        }

        [Benchmark]
        [Arguments(SimpleThemeMode.Dark)]
        [Arguments(SimpleThemeMode.Light)]
        public bool InitSimpleTheme(SimpleThemeMode mode)
        {
            UnitTestApplication.Current.Styles[0] = new SimpleTheme()
            {
                Mode = mode
            };
            return ((IResourceHost)UnitTestApplication.Current).TryGetResource("ThemeAccentColor", out _);
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
