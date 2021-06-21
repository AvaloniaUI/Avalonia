using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using Moq;

namespace Avalonia.Benchmarks.Themes
{
    [MemoryDiagnoser]
    public class FluentBenchmark
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        public FluentBenchmark()
        {
            _app = CreateApp();
            _root = new TestRoot(true, null)
            {
                Renderer = new NullRenderer()
            };

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        [Benchmark]
        public void RepeatButton()
        {
            var button = new RepeatButton();
            _root.Child = button;
            _root.LayoutManager.ExecuteLayoutPass();
        }

        private static IDisposable CreateApp()
        {
            var services = new TestServices(
                assetLoader: new AssetLoader(),
                globalClock: new MockGlobalClock(),
                platform: new AppBuilder().RuntimePlatform,
                renderInterface: new MockPlatformRenderInterface(),
                standardCursorFactory: Mock.Of<ICursorFactory>(),
                styler: new Styler(),
                theme: () => LoadFluentTheme(),
                threadingInterface: new NullThreadingPlatform(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl(),
                windowingPlatform: new MockWindowingPlatform());

            return UnitTestApplication.Start(services);
        }

        private static Styles LoadFluentTheme()
        {
            AssetLoader.RegisterResUriParsers();
            return new Styles
            {
                new Avalonia.Themes.Fluent.FluentTheme(new Uri("avares://Avalonia.Benchmarks"))
                {

                }
            };
        }
    }
}
