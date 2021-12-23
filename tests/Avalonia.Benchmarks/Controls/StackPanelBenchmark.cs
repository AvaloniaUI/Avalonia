using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Controls
{
    [MemoryDiagnoser]
    public class StackPanelBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private List<Border> _children;

        public StackPanelBenchmark()
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
            _children = Enumerable.Range(0, 50).Select(x => new Border()).ToList();
        }

        [Benchmark]
        public void Create_And_Layout_50_Children()
        {
            var target = new StackPanel();

            target.BeginInit();
            _root.Child = target;

            // Adding children one-by-one as this is how it's done in compiled XAML.
            foreach (var child in _children)
                target.Children.Add(child);

            target.EndInit();

            _root.LayoutManager.ExecuteLayoutPass();
            target.Children.Clear();
        }

        [Benchmark]
        public void Add_Remove_Children()
        {
            var target = new StackPanel();

            _root.Child = target;
            _root.LayoutManager.ExecuteLayoutPass();

            for (var i = 0; i < 100; ++i)
            {
                foreach (var child in _children)
                    target.Children.Add(child);

                for (var j = target.Children.Count - 1; j >= 0; --j)
                {
                    target.Children.RemoveAt(j);
                }
            }
        }

        public void Dispose() => _app.Dispose();
    }
}
