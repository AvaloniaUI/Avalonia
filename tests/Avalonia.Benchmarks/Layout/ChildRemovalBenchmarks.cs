using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for child removal from panels.
    /// Tests the cost of removing children from visual/logical tree which is
    /// critical for virtualization performance during scrolling.
    /// </summary>
    [MemoryDiagnoser]
    public class ChildRemovalBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private StackPanel _panel = null!;
        private List<Button> _children = null!;

        [Params(10, 50, 100)]
        public int ChildCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot { Renderer = new NullRenderer() };
            _panel = new StackPanel();
            _root.Child = _panel;

            _children = new List<Button>(ChildCount);
            for (var i = 0; i < ChildCount; i++)
            {
                var button = new Button { Width = 100, Height = 30, Content = $"Button {i}" };
                _children.Add(button);
            }

            // Initial layout
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Add all children before each iteration
            _panel.Children.Clear();
            foreach (var child in _children)
            {
                _panel.Children.Add(child);
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures removing all children one by one (worst case for virtualization).
        /// This simulates what happens when scrolling rapidly in a virtualized list.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RemoveChildren_OneByOne()
        {
            for (var i = _panel.Children.Count - 1; i >= 0; i--)
            {
                _panel.Children.RemoveAt(i);
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures removing all children at once with Clear().
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RemoveChildren_Clear()
        {
            _panel.Children.Clear();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures removing half the children from the beginning (scroll down scenario).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RemoveChildren_FirstHalf()
        {
            var halfCount = _panel.Children.Count / 2;
            for (var i = 0; i < halfCount; i++)
            {
                _panel.Children.RemoveAt(0);
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures removing half the children from the end (scroll up scenario).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RemoveChildren_LastHalf()
        {
            var halfCount = _panel.Children.Count / 2;
            for (var i = 0; i < halfCount; i++)
            {
                _panel.Children.RemoveAt(_panel.Children.Count - 1);
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of hiding children instead of removing (virtualization optimization).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HideChildren_AllVisible()
        {
            foreach (var child in _panel.Children)
            {
                child.IsVisible = false;
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures batch removal using RemoveRange.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RemoveChildren_Range()
        {
            _panel.Children.RemoveRange(0, _panel.Children.Count);
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
