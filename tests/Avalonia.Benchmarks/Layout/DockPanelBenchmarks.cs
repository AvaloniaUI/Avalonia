using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for DockPanel layout performance.
    /// Tests attached property lookups during layout.
    /// </summary>
    [MemoryDiagnoser]
    public class DockPanelBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _simpleRoot = null!;
        private TestRoot _complexRoot = null!;
        private DockPanel _simplePanel = null!;
        private DockPanel _complexPanel = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Simple DockPanel with 5 docked items
            _simpleRoot = new TestRoot { Renderer = new NullRenderer() };
            _simplePanel = CreateSimpleDockPanel();
            _simpleRoot.Child = _simplePanel;
            _simpleRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Complex DockPanel with many items and nested panels
            _complexRoot = new TestRoot { Renderer = new NullRenderer() };
            _complexPanel = CreateComplexDockPanel();
            _complexRoot.Child = _complexPanel;
            _complexRoot.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static DockPanel CreateSimpleDockPanel()
        {
            var panel = new DockPanel { LastChildFill = true };

            var top = new Button { Content = "Top", Height = 50 };
            DockPanel.SetDock(top, Dock.Top);
            panel.Children.Add(top);

            var bottom = new Button { Content = "Bottom", Height = 50 };
            DockPanel.SetDock(bottom, Dock.Bottom);
            panel.Children.Add(bottom);

            var left = new Button { Content = "Left", Width = 100 };
            DockPanel.SetDock(left, Dock.Left);
            panel.Children.Add(left);

            var right = new Button { Content = "Right", Width = 100 };
            DockPanel.SetDock(right, Dock.Right);
            panel.Children.Add(right);

            var center = new Button { Content = "Center" };
            panel.Children.Add(center);

            return panel;
        }

        private static DockPanel CreateComplexDockPanel()
        {
            var panel = new DockPanel { LastChildFill = true };

            // Add multiple items to each dock position
            for (var i = 0; i < 5; i++)
            {
                var top = new Button { Content = $"Top{i}", Height = 30 };
                DockPanel.SetDock(top, Dock.Top);
                panel.Children.Add(top);
            }

            for (var i = 0; i < 3; i++)
            {
                var left = new Button { Content = $"Left{i}", Width = 80 };
                DockPanel.SetDock(left, Dock.Left);
                panel.Children.Add(left);
            }

            for (var i = 0; i < 3; i++)
            {
                var right = new Button { Content = $"Right{i}", Width = 80 };
                DockPanel.SetDock(right, Dock.Right);
                panel.Children.Add(right);
            }

            for (var i = 0; i < 2; i++)
            {
                var bottom = new Button { Content = $"Bottom{i}", Height = 30 };
                DockPanel.SetDock(bottom, Dock.Bottom);
                panel.Children.Add(bottom);
            }

            // Nested DockPanel as center
            var innerPanel = new DockPanel();
            var innerTop = new Button { Content = "Inner Top", Height = 25 };
            DockPanel.SetDock(innerTop, Dock.Top);
            innerPanel.Children.Add(innerTop);
            innerPanel.Children.Add(new Button { Content = "Inner Center" });
            panel.Children.Add(innerPanel);

            return panel;
        }

        /// <summary>
        /// Measures simple DockPanel layout.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SimpleDockPanel_Measure()
        {
            _simplePanel.InvalidateMeasure();
            _simpleRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures complex DockPanel layout with many docked items.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ComplexDockPanel_Measure()
        {
            _complexPanel.InvalidateMeasure();
            _complexRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass for simple DockPanel.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SimpleDockPanel_FullLayout()
        {
            InvalidateRecursive(_simplePanel);
            _simpleRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass for complex DockPanel.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ComplexDockPanel_FullLayout()
        {
            InvalidateRecursive(_complexPanel);
            _complexRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures the cost of changing a dock property.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DockPanel_ChangeDockProperty()
        {
            if (_simplePanel.Children.Count > 0 && _simplePanel.Children[0] is Control child)
            {
                var original = DockPanel.GetDock(child);
                DockPanel.SetDock(child, Dock.Bottom);
                _simpleRoot.LayoutManager.ExecuteLayoutPass();
                DockPanel.SetDock(child, original);
                _simpleRoot.LayoutManager.ExecuteLayoutPass();
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
