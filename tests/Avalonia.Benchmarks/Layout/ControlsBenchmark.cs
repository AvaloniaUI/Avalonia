using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    [MemoryDiagnoser]
    public class ControlsBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        public ControlsBenchmark()
        {
            _app = UnitTestApplication.Start(
                TestServices.StyledWindow.With(
                    renderInterface: new NullRenderingPlatform(),
                    threadingInterface: new NullThreadingPlatform(),
                    standardCursorFactory: new NullCursorFactory()));

            _root = new TestRoot(true, null)
            {
                Renderer = new NullRenderer()
            };

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateCalendar()
        {
            var calendar = new Calendar();

            _root.Child = calendar;

            _root.LayoutManager.ExecuteLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateButton()
        {
            var button = new Button();

            _root.Child = button;

            _root.LayoutManager.ExecuteLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateTextBox()
        {
            var textBox = new TextBox();

            _root.Child = textBox;

            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
