using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent2;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Themes
{
    [MemoryDiagnoser]
    public class Fluent2ThemeBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly object[] _themeResourceKeys;
        private readonly Fluent2Theme _reusableTheme;

        public Fluent2ThemeBenchmark()
        {
            AssetLoader.RegisterResUriParsers();

            _app = UnitTestApplication.Start(TestServices.StyledWindow.With(theme: () => null));

            // Theme dependant resources: mostly brushes and colors.
            _themeResourceKeys = new Fluent2Theme().Resources.ThemeDictionaries.Values
                .OfType<IResourceDictionary>()
                .SelectMany(d => d.Keys)
                .Distinct()
                .ToArray();

            _reusableTheme = new Fluent2Theme();
            ResolveThemeResources(_reusableTheme);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Fluent2Theme CreateTheme()
        {
            return new Fluent2Theme();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int CreateThemeAndResolveThemeResources()
        {
            return ResolveThemeResources(new Fluent2Theme());
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int LookupResolvedThemeResources()
        {
            return ResolveThemeResources(_reusableTheme);
        }

        private int ResolveThemeResources(Fluent2Theme theme)
        {
            var count = 0;
            foreach (var key in _themeResourceKeys)
            {
                if (theme.TryGetResource(key, ThemeVariant.Light, out _))
                    count++;
            }
            return count;
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
