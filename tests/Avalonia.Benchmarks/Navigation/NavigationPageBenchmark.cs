using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
    /// Measures the cost of creating a NavigationPage and applying its template.
    /// </summary>
    [MemoryDiagnoser]
    public class NavigationPageCreationBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        public NavigationPageCreationBenchmark()
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
            _root.Child = new NavigationPage { PageTransition = null };
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        public void Dispose() => _app.Dispose();
    }

    /// <summary>
    /// Measures push and pop on a NavigationPage that already has two pages on its stack.
    /// Transitions are disabled so only the stack-management and layout cost is captured.
    /// </summary>
    [MemoryDiagnoser]
    public class NavigationPageStackBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private NavigationPage _nav = null!;

        public NavigationPageStackBenchmark()
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
            _nav = new NavigationPage { PageTransition = null };
            _root.Child = _nav;
            _root.LayoutManager.ExecuteLayoutPass();
            _nav.PushAsync(new ContentPage { Header = "Root" }).GetAwaiter().GetResult();
            _nav.PushAsync(new ContentPage { Header = "Page 2" }).GetAwaiter().GetResult();
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Push a page onto a stack that already has two entries (depth 2 → 3).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task Push()
        {
            await _nav.PushAsync(new ContentPage { Header = "Page 3" });
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Pop the top page from a stack with two entries (depth 2 → 1).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task Pop()
        {
            await _nav.PopAsync();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose() => _app.Dispose();
    }
}
