using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for StackPanel layout performance.
    /// Compares horizontal and vertical orientations with various child counts.
    /// </summary>
    [MemoryDiagnoser]
    public class StackPanelBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _verticalRoot = null!;
        private TestRoot _horizontalRoot = null!;
        private TestRoot _nestedRoot = null!;
        private StackPanel _verticalPanel = null!;
        private StackPanel _horizontalPanel = null!;
        private StackPanel _nestedPanel = null!;

        [Params(10, 50, 200)]
        public int ChildCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Vertical StackPanel
            _verticalRoot = new TestRoot { Renderer = new NullRenderer() };
            _verticalPanel = new StackPanel { Orientation = Orientation.Vertical };
            PopulatePanel(_verticalPanel, ChildCount);
            _verticalRoot.Child = _verticalPanel;
            _verticalRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Horizontal StackPanel
            _horizontalRoot = new TestRoot { Renderer = new NullRenderer() };
            _horizontalPanel = new StackPanel { Orientation = Orientation.Horizontal };
            PopulatePanel(_horizontalPanel, ChildCount);
            _horizontalRoot.Child = _horizontalPanel;
            _horizontalRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Nested StackPanels
            _nestedRoot = new TestRoot { Renderer = new NullRenderer() };
            _nestedPanel = CreateNestedStackPanels(5, ChildCount / 5);
            _nestedRoot.Child = _nestedPanel;
            _nestedRoot.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static void PopulatePanel(Panel panel, int count)
        {
            for (var i = 0; i < count; i++)
            {
                panel.Children.Add(new Button
                {
                    Width = 100,
                    Height = 30,
                    Content = $"Button {i}"
                });
            }
        }

        private static StackPanel CreateNestedStackPanels(int depth, int childrenPerLevel)
        {
            var root = new StackPanel { Orientation = Orientation.Vertical };
            var current = root;

            for (var d = 0; d < depth; d++)
            {
                for (var i = 0; i < childrenPerLevel - 1; i++)
                {
                    current.Children.Add(new Button
                    {
                        Width = 100,
                        Height = 30,
                        Content = $"D{d}-B{i}"
                    });
                }

                if (d < depth - 1)
                {
                    var next = new StackPanel
                    {
                        Orientation = d % 2 == 0 ? Orientation.Horizontal : Orientation.Vertical
                    };
                    current.Children.Add(next);
                    current = next;
                }
                else
                {
                    current.Children.Add(new Button
                    {
                        Width = 100,
                        Height = 30,
                        Content = $"D{d}-Last"
                    });
                }
            }

            return root;
        }

        /// <summary>
        /// Measures vertical StackPanel layout.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void VerticalStackPanel_Measure()
        {
            _verticalPanel.InvalidateMeasure();
            _verticalRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures horizontal StackPanel layout.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HorizontalStackPanel_Measure()
        {
            _horizontalPanel.InvalidateMeasure();
            _horizontalRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures nested StackPanels layout.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NestedStackPanels_Measure()
        {
            _nestedPanel.InvalidateMeasure();
            _nestedRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass for vertical StackPanel with all children invalidated.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void VerticalStackPanel_FullLayout()
        {
            InvalidateRecursive(_verticalPanel);
            _verticalRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures single child change in StackPanel.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void VerticalStackPanel_SingleChildChange()
        {
            if (_verticalPanel.Children.Count > 0 && _verticalPanel.Children[0] is Button button)
            {
                button.Width = 120;
                _verticalRoot.LayoutManager.ExecuteLayoutPass();
                button.Width = 100;
                _verticalRoot.LayoutManager.ExecuteLayoutPass();
            }
        }

        private static void InvalidateRecursive(Control control)
        {
            if (control is Layoutable layoutable)
            {
                SetIsMeasureValid(layoutable, false);
                SetIsArrangeValid(layoutable, false);
            }

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    InvalidateRecursive(child);
                }
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(Layoutable.IsMeasureValid))]
        private static extern void SetIsMeasureValid(Layoutable layoutable, bool value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(Layoutable.IsArrangeValid))]
        private static extern void SetIsArrangeValid(Layoutable layoutable, bool value);

        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
