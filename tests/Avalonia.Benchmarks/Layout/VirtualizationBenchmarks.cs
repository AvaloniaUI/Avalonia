using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for VirtualizingStackPanel performance.
    /// Tests element recycling, scroll performance, and viewport calculations.
    /// </summary>
    [MemoryDiagnoser]
    public class VirtualizationBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _root = null!;
        private ListBox _listBox = null!;
        private ScrollViewer _scrollViewer = null!;
        private List<string> _items = null!;

        [Params(1000, 10000)]
        public int ItemCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot
            {
                Renderer = new NullRenderer(),
                ClientSize = new Size(400, 600)
            };

            _items = new List<string>(ItemCount);
            for (var i = 0; i < ItemCount; i++)
            {
                _items.Add($"Item {i}");
            }

            _listBox = new ListBox
            {
                ItemsSource = _items,
                Width = 400,
                Height = 600
            };

            _root.Child = _listBox;
            _root.LayoutManager.ExecuteInitialLayoutPass();

            // Get the scroll viewer
            _scrollViewer = _listBox.Scroll as ScrollViewer ?? 
                           FindScrollViewer(_listBox);
        }

        private static ScrollViewer? FindScrollViewer(Control control)
        {
            if (control is ScrollViewer sv)
                return sv;

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var result = FindScrollViewer(child);
                    if (result != null)
                        return result;
                }
            }
            else if (control is Decorator decorator && decorator.Child != null)
            {
                return FindScrollViewer(decorator.Child);
            }
            else if (control is ContentControl cc && cc.Content is Control content)
            {
                return FindScrollViewer(content);
            }

            return null;
        }

        /// <summary>
        /// Measures initial layout of virtualized list.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InitialLayout()
        {
            _listBox.InvalidateMeasure();
            _root.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Simulates scrolling down by small increment.
        /// Tests typical user scrolling behavior.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDown_SmallIncrement()
        {
            if (_scrollViewer != null)
            {
                var currentOffset = _scrollViewer.Offset;
                _scrollViewer.Offset = currentOffset.WithY(currentOffset.Y + 30);
                _root.LayoutManager.ExecuteLayoutPass();
                
                // Reset for next iteration
                _scrollViewer.Offset = currentOffset;
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Simulates scrolling down by large increment (page).
        /// Tests element recycling when many items change.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDown_LargeIncrement()
        {
            if (_scrollViewer != null)
            {
                var currentOffset = _scrollViewer.Offset;
                _scrollViewer.Offset = currentOffset.WithY(currentOffset.Y + 500);
                _root.LayoutManager.ExecuteLayoutPass();
                
                // Reset for next iteration
                _scrollViewer.Offset = currentOffset;
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Simulates scrolling up by small increment.
        /// Tests O(n) insertion issue in RealizedStackElements.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollUp_SmallIncrement()
        {
            if (_scrollViewer != null)
            {
                // First scroll down to have room to scroll up
                var startOffset = new Vector(0, 300);
                _scrollViewer.Offset = startOffset;
                _root.LayoutManager.ExecuteLayoutPass();

                // Now scroll up
                _scrollViewer.Offset = startOffset.WithY(startOffset.Y - 30);
                _root.LayoutManager.ExecuteLayoutPass();
                
                // Reset
                _scrollViewer.Offset = new Vector(0, 0);
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Simulates rapid bidirectional scrolling.
        /// Tests element recycling under stress.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RapidBidirectionalScroll()
        {
            if (_scrollViewer != null)
            {
                var maxScroll = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
                
                // Scroll pattern: down, down, up, down, up, up
                double[] offsets = { 100, 300, 200, 500, 400, 300 };
                
                foreach (var offset in offsets)
                {
                    _scrollViewer.Offset = new Vector(0, Math.Min(offset, maxScroll));
                    _root.LayoutManager.ExecuteLayoutPass();
                }
                
                // Reset
                _scrollViewer.Offset = new Vector(0, 0);
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        /// <summary>
        /// Scrolls to end and back to beginning.
        /// Tests full virtualization cycle.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollToEndAndBack()
        {
            if (_scrollViewer != null)
            {
                var maxScroll = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
                
                // Scroll to end
                _scrollViewer.Offset = new Vector(0, maxScroll);
                _root.LayoutManager.ExecuteLayoutPass();
                
                // Scroll back to start
                _scrollViewer.Offset = new Vector(0, 0);
                _root.LayoutManager.ExecuteLayoutPass();
            }
        }

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
