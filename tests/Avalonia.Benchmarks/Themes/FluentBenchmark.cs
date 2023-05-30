using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
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

        [Benchmark()]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateButton()
        {
            var button = new Button();
            _root.Child = button;
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        private static IDisposable CreateApp()
        {
            var services = new TestServices(
                theme: () => LoadFluentTheme());

            return UnitTestApplication.Start(services);
        }

        private static Styles LoadFluentTheme()
        {
            AssetLoader.RegisterResUriParsers();
            return new Styles
            {
                new Avalonia.Themes.Fluent.FluentTheme()
                {

                }
            };
        }
    }
}
