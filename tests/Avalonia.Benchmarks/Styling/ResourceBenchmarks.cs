using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.PlatformSupport;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using Moq;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class ResourceBenchmarks : IDisposable
    {
        private readonly Control _searchStart;
        private readonly IDisposable _app;
    
        private static IDisposable CreateApp()
        {
            var services = new TestServices(
                assetLoader: new AssetLoader(),
                globalClock: new MockGlobalClock(),
                platform: new AppBuilder().RuntimePlatform,
                renderInterface: new MockPlatformRenderInterface(),
                standardCursorFactory: Mock.Of<ICursorFactory>(),
                styler: new Styler(),
                theme: () => LoadTheme(),
                threadingInterface: new NullThreadingPlatform(),
                fontManagerImpl: new MockFontManagerImpl(),
                textShaperImpl: new MockTextShaperImpl(),
                windowingPlatform: new MockWindowingPlatform());

            return UnitTestApplication.Start(services);
        }
    
        private static Styles LoadTheme()
        {
            AssetLoader.RegisterResUriParsers();
            
            var hostStyle = new Style();
            
            hostStyle.Resources.Add("test", null);
            
            return new Styles
            {
                hostStyle,
                new FluentTheme(new Uri("avares://Avalonia.Benchmarks"))
            };
        }

        public void Dispose()
        {
            _app.Dispose();
        }
        
        public ResourceBenchmarks()
        {
            _searchStart = new Button();

            _app = CreateApp();
        
            Decorator root = new TestRoot(true, null)
            {
                Renderer = new NullRenderer()
            };
        
            var current = root;
            
            for (int i = 0; i < 10; i++)
            {
                var child = new Decorator();

                current.Child = child;

                current = child;
            }

            current.Child = _searchStart;
        }
        
        [Benchmark(Baseline = true)]
        public void FindResource()
        {
            for (int i = 0; i < 100; ++i)
            {
                _searchStart.FindResource("test");
            }
        }
    }
}
