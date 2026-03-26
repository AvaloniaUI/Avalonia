using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Navigation
{
    /// <summary>
    /// Measures the cost of creating a CarouselPage and applying its template
    /// as the number of pages grows.
    /// </summary>
    [MemoryDiagnoser]
    public class CarouselPageCreationBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        [Params(2, 5, 10)]
        public int PageCount { get; set; }

        public CarouselPageCreationBenchmark()
        {
            AssetLoader.RegisterResUriParsers();
            _app = UnitTestApplication.Start(TestServices.StyledWindow.With(
                theme: () => new Styles { new FluentTheme() }));
            _root = new TestRoot(true, null) { Renderer = new NullRenderer() };
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Create()
        {
            var cp = new CarouselPage { PageTransition = null };
            for (var i = 0; i < PageCount; i++)
                ((IList)cp.Pages!).Add(new ContentPage { Header = $"Page {i}" });
            _root.Child = cp;
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        public void Dispose() => _app.Dispose();
    }

    /// <summary>
    /// Measures the cost of navigating between pages on a CarouselPage with five pages (no transition).
    /// </summary>
    [MemoryDiagnoser]
    public class CarouselPageNavigationBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private CarouselPage _carouselPage = null!;

        public CarouselPageNavigationBenchmark()
        {
            AssetLoader.RegisterResUriParsers();
            _app = UnitTestApplication.Start(TestServices.StyledWindow.With(
                theme: () => new Styles { new FluentTheme() }));
            _root = new TestRoot(true, null) { Renderer = new NullRenderer() };
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [IterationSetup]
        public void SetupIteration()
        {
            _carouselPage = new CarouselPage { PageTransition = null };
            for (var i = 0; i < 5; i++)
                ((IList)_carouselPage.Pages!).Add(new ContentPage { Header = $"Page {i}" });
            _root.Child = _carouselPage;
            _root.LayoutManager.ExecuteLayoutPass();
            _carouselPage.SelectedIndex = 0;
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Navigate forward from page 0 to page 1 (no transition).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NavigateNext()
        {
            _carouselPage.SelectedIndex = 1;
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose() => _app.Dispose();
    }
}
