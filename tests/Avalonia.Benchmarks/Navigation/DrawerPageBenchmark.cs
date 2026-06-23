using System;
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
    /// Measures the cost of creating a DrawerPage and applying its template.
    /// </summary>
    [MemoryDiagnoser]
    public class DrawerPageCreationBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        public DrawerPageCreationBenchmark()
        {
            AssetLoader.RegisterResUriParsers();
            _app = UnitTestApplication.Start(TestServices.StyledWindow.With(
                theme: () => new Styles { new FluentTheme() },
                globalClock: new MockGlobalClock()));
            _root = new TestRoot(true, null) { Renderer = new NullRenderer() };
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Create()
        {
            _root.Child = new DrawerPage
            {
                Drawer = new TextBlock { Text = "Drawer" },
                Content = new TextBlock { Text = "Content" }
            };
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        public void Dispose() => _app.Dispose();
    }

    /// <summary>
    /// Measures the cost of opening and then closing a DrawerPage in a single iteration.
    /// The DrawerPage starts closed each iteration; the benchmark performs one open and
    /// one close so both layout passes are captured symmetrically.
    /// </summary>
    [MemoryDiagnoser]
    public class DrawerPageToggleBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private DrawerPage _drawer = null!;

        public DrawerPageToggleBenchmark()
        {
            AssetLoader.RegisterResUriParsers();
            _app = UnitTestApplication.Start(TestServices.StyledWindow.With(
                theme: () => new Styles { new FluentTheme() },
                globalClock: new MockGlobalClock()));
            _root = new TestRoot(true, null) { Renderer = new NullRenderer() };
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [IterationSetup]
        public void SetupIteration()
        {
            _drawer = new DrawerPage
            {
                Drawer = new TextBlock { Text = "Drawer" },
                Content = new TextBlock { Text = "Content" },
                IsOpen = false
            };
            _root.Child = _drawer;
            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Open the drawer then close it; captures the full toggle cycle cost.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OpenAndClose()
        {
            _drawer.IsOpen = true;
            _root.LayoutManager.ExecuteLayoutPass();

            _drawer.IsOpen = false;
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose() => _app.Dispose();
    }
}
