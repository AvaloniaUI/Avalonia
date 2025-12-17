using System;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    [MemoryDiagnoser]
    public class VisualTreeAttachmentBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Border? _subtree;

        [Params(5, 10, 20)]
        public int TreeDepth { get; set; }

        [Params(1, 3)]
        public int ChildrenPerLevel { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };
            _subtree = CreateSubtree(TreeDepth, ChildrenPerLevel);
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static Border CreateSubtree(int depth, int childrenPerLevel)
        {
            var root = new Border { Width = 100, Height = 100 };
            
            if (depth > 1)
            {
                if (childrenPerLevel == 1)
                {
                    root.Child = CreateSubtree(depth - 1, childrenPerLevel);
                }
                else
                {
                    var panel = new StackPanel();
                    for (int i = 0; i < childrenPerLevel; i++)
                    {
                        panel.Children.Add(CreateSubtree(depth - 1, childrenPerLevel));
                    }
                    root.Child = panel;
                }
            }

            return root;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _subtree = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark attaching a subtree to the visual tree
        /// </summary>
        [Benchmark(Baseline = true)]
        public void AttachSubtree()
        {
            _root!.Child = _subtree;
            _root.Child = null;
        }

        /// <summary>
        /// Benchmark moving a subtree between parents
        /// </summary>
        [Benchmark]
        public void MoveSubtreeBetweenParents()
        {
            var parent1 = new Border();
            var parent2 = new Border();
            _root!.Child = parent1;

            parent1.Child = _subtree;
            parent1.Child = null;
            parent2.Child = _subtree;
            parent2.Child = null;

            _root.Child = null;
        }

        /// <summary>
        /// Benchmark rapid attach/detach cycles
        /// </summary>
        [Benchmark]
        public void RapidAttachDetachCycles()
        {
            for (int i = 0; i < 5; i++)
            {
                _root!.Child = _subtree;
                _root.Child = null;
            }
        }

        /// <summary>
        /// Benchmark attaching multiple separate subtrees
        /// </summary>
        [Benchmark]
        public void AttachMultipleSubtrees()
        {
            var panel = new StackPanel();
            _root!.Child = panel;

            for (int i = 0; i < 3; i++)
            {
                var subtree = CreateSubtree(TreeDepth / 2, ChildrenPerLevel);
                panel.Children.Add(subtree);
            }

            panel.Children.Clear();
            _root.Child = null;
        }
    }
}
