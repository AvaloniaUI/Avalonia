using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class ContainerQueryBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Border? _container;
        private Border? _target;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            // Create a container with sizing enabled
            _container = new Border
            {
                Width = 500,
                Height = 500,
            };
            Container.SetName(_container, "myContainer");
            Container.SetSizing(_container, ContainerSizing.Width);

            _target = new Border
            {
                Width = 100,
                Height = 100
            };

            _container.Child = _target;
            _root.Child = _container;

            // Add container query style - exercises the optimized GetContainer method
            var containerQuery = new ContainerQuery(q => q.Width(StyleQueryComparisonOperator.GreaterThan, 200));
            containerQuery.Children.Add(new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, Brushes.Red)
                }
            });
            _root.Styles.Add(containerQuery);

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark container size changes - exercises optimized GetContainer foreach loop
        /// </summary>
        [Benchmark(Baseline = true)]
        public void ContainerSizeChange()
        {
            _container!.Width = 600;
            _root!.LayoutManager.ExecuteLayoutPass();
            _container.Width = 100;
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Benchmark rapid container size changes
        /// </summary>
        [Benchmark]
        public void ContainerSizeChange_Rapid()
        {
            for (int i = 0; i < 10; i++)
            {
                _container!.Width = 200 + (i * 50);
                _root!.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Benchmark adding/removing from container - triggers style re-evaluation
        /// </summary>
        [Benchmark]
        public void AddRemoveFromContainer()
        {
            _container!.Child = null;
            _root!.LayoutManager.ExecuteLayoutPass();
            _container.Child = _target;
            _root.LayoutManager.ExecuteLayoutPass();
        }
    }
}
