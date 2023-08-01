using System;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Platform;
using Avalonia.Styling;
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
                assetLoader: new StandardAssetLoader(),
                globalClock: new MockGlobalClock(),
                platform: new StandardRuntimePlatform(),
                standardCursorFactory: Mock.Of<ICursorFactory>(),
                theme: () => CreateTheme(),
                windowingPlatform: new MockWindowingPlatform());

            return UnitTestApplication.Start(services);
        }
    
        private static Styles CreateTheme()
        {
            AssetLoader.RegisterResUriParsers();
            
            var preHost = new Style();
            preHost.Resources.Add("preTheme", null);
            
            var postHost = new Style();
            postHost.Resources.Add("postTheme", null);

            return new Styles
            {
                preHost,
                new TestStyles(50, 3, 5, 0),
                postHost
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

        private const int LookupCount = 100;
        
        [Benchmark]
        public void FindPreResource()
        {
            for (int i = 0; i < LookupCount; ++i)
            {
                _searchStart.FindResource("preTheme");
            }
        }
        
        [Benchmark]
        public void FindPostResource()
        {
            for (int i = 0; i < LookupCount; ++i)
            {
                _searchStart.FindResource("postTheme");
            }
        }
        
        [Benchmark]
        public void FindNotExistingResource()
        {
            for (int i = 0; i < LookupCount; ++i)
            {
                _searchStart.FindResource("notPresent");
            }
        }
    }
}
