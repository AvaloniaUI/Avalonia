using System;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    [MemoryDiagnoser]
    public class VisualLocatorBenchmarks
    {
        private TestRoot _root = null!;
        private Visual _deepChild = null!;
        private IDisposable _app = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot();
            
            // Create a deep visual tree
            Visual current = _root;
            for (int i = 0; i < 20; i++)
            {
                var child = new Border();
                if (current is Border border)
                {
                    border.Child = child;
                }
                else if (current is TestRoot root)
                {
                    root.Child = child;
                }
                current = child;
            }
            _deepChild = current;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _app?.Dispose();
        }

        [Benchmark(Baseline = true)]
        public Visual? GetVisualParent()
        {
            return _deepChild.VisualParent;
        }

        [Benchmark]
        public Visual? FindVisualAncestor_Direct()
        {
            return _deepChild.FindAncestorOfType<TestRoot>();
        }

        [Benchmark]
        public int CountVisualAncestors()
        {
            int count = 0;
            foreach (var _ in _deepChild.GetVisualAncestors())
            {
                count++;
            }
            return count;
        }

        [Benchmark]
        public int CountSelfAndVisualAncestors()
        {
            int count = 0;
            foreach (var _ in _deepChild.GetSelfAndVisualAncestors())
            {
                count++;
            }
            return count;
        }

        [Benchmark]
        public int SortByZIndex_SmallList()
        {
            // Create a small list of visuals
            var children = new Visual[5];
            for (int i = 0; i < 5; i++)
            {
                children[i] = new Border { ZIndex = 5 - i };
            }

            int sum = 0;
            foreach (var visual in children.SortByZIndex())
            {
                sum += visual.ZIndex;
            }
            return sum;
        }

        [Benchmark]
        public int SortByZIndex_LargeList()
        {
            // Create a larger list of visuals
            var children = new Visual[20];
            for (int i = 0; i < 20; i++)
            {
                children[i] = new Border { ZIndex = 20 - i };
            }

            int sum = 0;
            foreach (var visual in children.SortByZIndex())
            {
                sum += visual.ZIndex;
            }
            return sum;
        }
    }
}
