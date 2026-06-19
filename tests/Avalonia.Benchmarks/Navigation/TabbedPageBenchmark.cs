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
    /// Measures the cost of creating a TabbedPage and applying its template
    /// as the number of tabs grows.
    /// </summary>
    [MemoryDiagnoser]
    public class TabbedPageCreationBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        [Params(2, 5, 10)]
        public int TabCount { get; set; }

        public TabbedPageCreationBenchmark()
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
            var tp = new TabbedPage { PageTransition = null };
            for (var i = 0; i < TabCount; i++)
                ((IList)tp.Pages!).Add(new ContentPage { Header = $"Tab {i}" });
            _root.Child = tp;
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        public void Dispose() => _app.Dispose();
    }

    /// <summary>
    /// Measures the cost of switching the selected tab on a TabbedPage with five tabs (no transition).
    /// </summary>
    [MemoryDiagnoser]
    public class TabbedPageSwitchBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private TabbedPage _tabbedPage = null!;

        public TabbedPageSwitchBenchmark()
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
            _tabbedPage = new TabbedPage { PageTransition = null };
            for (var i = 0; i < 5; i++)
                ((IList)_tabbedPage.Pages!).Add(new ContentPage { Header = $"Tab {i}" });
            _root.Child = _tabbedPage;
            _root.LayoutManager.ExecuteLayoutPass();
            _tabbedPage.SelectedIndex = 0;
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Switch from tab 0 to tab 1 (no transition).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SwitchTab()
        {
            _tabbedPage.SelectedIndex = 1;
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose() => _app.Dispose();
    }
}
