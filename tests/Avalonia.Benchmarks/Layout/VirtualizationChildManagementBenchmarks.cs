using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for virtualization panel child management.
    /// Simulates real-world scrolling scenarios with add/remove operations.
    /// </summary>
    [MemoryDiagnoser]
    public class VirtualizationChildManagementBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private StackPanel _panel = null!;
        private List<Button> _pooledControls = null!;

        [Params(20, 50)]
        public int VisibleCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot { Renderer = new NullRenderer() };
            _panel = new StackPanel();
            _root.Child = _panel;

            // Create a pool of controls (simulating recycled containers)
            _pooledControls = new List<Button>(VisibleCount * 3);
            for (var i = 0; i < VisibleCount * 3; i++)
            {
                _pooledControls.Add(new Button { Width = 100, Height = 30, Content = $"Item {i}" });
            }

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Set up panel with VisibleCount children
            _panel.Children.Clear();
            for (var i = 0; i < VisibleCount; i++)
            {
                _panel.Children.Add(_pooledControls[i]);
            }
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Simulates scrolling down: remove first few items, add new items at end.
        /// This is what happens in VirtualizingStackPanel during scroll-down.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDown_RemoveFirstAddLast()
        {
            const int scrollAmount = 5;
            
            // Remove items from the beginning (scrolled off top)
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.RemoveAt(0);
            }
            
            // Add new items at the end (scrolled into view from bottom)
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.Add(_pooledControls[VisibleCount + i]);
            }
            
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Simulates scrolling up: remove last few items, add new items at beginning.
        /// This is what happens in VirtualizingStackPanel during scroll-up.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollUp_RemoveLastAddFirst()
        {
            const int scrollAmount = 5;
            
            // Remove items from the end (scrolled off bottom)
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.RemoveAt(_panel.Children.Count - 1);
            }
            
            // Add new items at the beginning (scrolled into view from top)
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.Insert(0, _pooledControls[VisibleCount * 2 + i]);
            }
            
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Uses IsVisible to hide/show instead of adding/removing from visual tree.
        /// This is a potential optimization for virtualization.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDown_UsingVisibility()
        {
            const int scrollAmount = 5;
            
            // Hide first few items
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children[i].IsVisible = false;
            }
            
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Batch remove and add using RemoveRange/AddRange.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDown_BatchOperations()
        {
            const int scrollAmount = 5;
            
            // Remove range from beginning
            _panel.Children.RemoveRange(0, scrollAmount);
            
            // Add range at end
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.Add(_pooledControls[VisibleCount + i]);
            }
            
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Single layout pass after all modifications.
        /// Tests deferred invalidation benefit.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SingleLayoutPass_MultipleChanges()
        {
            const int scrollAmount = 5;
            
            // Remove items
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.RemoveAt(0);
            }
            
            // Add items  
            for (var i = 0; i < scrollAmount; i++)
            {
                _panel.Children.Add(_pooledControls[VisibleCount + i]);
            }
            
            // Only one layout pass
            _root.LayoutManager.ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
