using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    [MemoryDiagnoser]
    public class CalendarBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        public CalendarBenchmark()
        {
            _app = UnitTestApplication.Start(
                TestServices.StyledWindow.With(
                    renderInterface: new NullRenderingPlatform(),
                    threadingInterface: new NullThreadingPlatform()));

            _root = new TestRoot(true, null)
            {
                Renderer = new NullRenderer()
            };

            _root.LayoutManager.ExecuteInitialLayoutPass(_root);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateCalendar()
        {
            var calendar = new Calendar();

            _root.Child = calendar;

            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
