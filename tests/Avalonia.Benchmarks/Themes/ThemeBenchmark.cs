using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Styling;
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
        [Arguments("avares://Avalonia.Themes.Fluent/FluentDark.xaml")]
        [Arguments("avares://Avalonia.Themes.Fluent/FluentLight.xaml")]
        public bool InitFluentTheme(string themeUri)
        {
            UnitTestApplication.Current.Styles[0] = new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Benchmarks"))
            {
                Source = new Uri(themeUri)
            };
            return ((IResourceHost)UnitTestApplication.Current).TryGetResource("SystemAccentColor", out _);
        }

        [Benchmark]
        [Arguments("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")]
        [Arguments("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")]
        public bool InitDefaultTheme(string themeUri)
        {
            UnitTestApplication.Current.Styles[0] = new Styles
            {
                new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Benchmarks"))
                {
                    Source = new Uri(themeUri)
                },
                new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Benchmarks"))
                {
                    Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
                }
            };
            return ((IResourceHost)UnitTestApplication.Current).TryGetResource("ThemeAccentColor", out _);
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
