using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks for Grid panel layout performance.
    /// Tests star sizing, spanning cells, and dictionary/hashtable performance.
    /// </summary>
    [MemoryDiagnoser]
    public class GridBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _simpleGridRoot = null!;
        private TestRoot _starGridRoot = null!;
        private TestRoot _spanGridRoot = null!;
        private TestRoot _complexGridRoot = null!;
        private Grid _simpleGrid = null!;
        private Grid _starGrid = null!;
        private Grid _spanGrid = null!;
        private Grid _complexGrid = null!;

        [Params(5, 10, 20)]
        public int GridSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Simple grid with fixed rows/columns
            (_simpleGridRoot, _simpleGrid) = CreateSimpleGrid(GridSize);

            // Grid with star sizing
            (_starGridRoot, _starGrid) = CreateStarGrid(GridSize);

            // Grid with spanning cells
            (_spanGridRoot, _spanGrid) = CreateSpanGrid(GridSize);

            // Complex grid with mixed sizing and spans
            (_complexGridRoot, _complexGrid) = CreateComplexGrid(GridSize);
        }

        private static (TestRoot, Grid) CreateSimpleGrid(int size)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            var grid = new Grid();

            for (var i = 0; i < size; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition(50, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(100, GridUnitType.Pixel));
            }

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var button = new Button { Content = $"{row},{col}" };
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    grid.Children.Add(button);
                }
            }

            root.Child = grid;
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (root, grid);
        }

        private static (TestRoot, Grid) CreateStarGrid(int size)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            var grid = new Grid();

            for (var i = 0; i < size; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            }

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var button = new Button { Content = $"{row},{col}" };
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    grid.Children.Add(button);
                }
            }

            root.Child = grid;
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (root, grid);
        }

        private static (TestRoot, Grid) CreateSpanGrid(int size)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            var grid = new Grid();

            for (var i = 0; i < size; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            }

            // Add spanning cells
            for (var row = 0; row < size; row += 2)
            {
                for (var col = 0; col < size; col += 2)
                {
                    var button = new Button
                    {
                        Content = $"Span {row},{col}",
                        Width = 150,
                        Height = 60
                    };
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    Grid.SetRowSpan(button, Math.Min(2, size - row));
                    Grid.SetColumnSpan(button, Math.Min(2, size - col));
                    grid.Children.Add(button);
                }
            }

            root.Child = grid;
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (root, grid);
        }

        private static (TestRoot, Grid) CreateComplexGrid(int size)
        {
            var root = new TestRoot { Renderer = new NullRenderer() };
            var grid = new Grid();

            // Mixed row/column definitions
            for (var i = 0; i < size; i++)
            {
                var remainder = i % 3;
                var rowUnit = remainder switch
                {
                    0 => GridUnitType.Auto,
                    1 => GridUnitType.Star,
                    _ => GridUnitType.Pixel
                };
                var colUnit = remainder switch
                {
                    0 => GridUnitType.Pixel,
                    1 => GridUnitType.Auto,
                    _ => GridUnitType.Star
                };

                grid.RowDefinitions.Add(new RowDefinition(remainder == 2 ? 50 : 1, rowUnit));
                grid.ColumnDefinitions.Add(new ColumnDefinition(remainder == 0 ? 100 : 1, colUnit));
            }

            // Add children with varying spans
            var childIndex = 0;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    if (row + col > 0 && (row + col) % 3 == 0)
                    {
                        // Skip some cells that will be covered by spans
                        continue;
                    }

                    var button = new Button
                    {
                        Content = $"C{childIndex++}",
                        MinWidth = 30,
                        MinHeight = 20
                    };
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);

                    if ((row + col) % 5 == 0 && row < size - 1)
                    {
                        Grid.SetRowSpan(button, 2);
                    }
                    if ((row + col) % 7 == 0 && col < size - 1)
                    {
                        Grid.SetColumnSpan(button, 2);
                    }

                    grid.Children.Add(button);
                }
            }

            root.Child = grid;
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (root, grid);
        }

        /// <summary>
        /// Measures a simple fixed-size grid.
        /// Baseline for grid performance.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SimpleGrid_Measure()
        {
            _simpleGrid.InvalidateMeasure();
            _simpleGridRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures a grid with star sizing.
        /// Tests star resolution algorithm performance.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StarGrid_Measure()
        {
            _starGrid.InvalidateMeasure();
            _starGridRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures a grid with spanning cells.
        /// Tests span storage performance (Hashtable boxing issue).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SpanGrid_Measure()
        {
            _spanGrid.InvalidateMeasure();
            _spanGridRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Measures a complex grid with mixed sizing and spans.
        /// Tests real-world grid performance.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ComplexGrid_Measure()
        {
            _complexGrid.InvalidateMeasure();
            _complexGridRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Full layout pass (measure + arrange) for complex grid.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ComplexGrid_FullLayout()
        {
            InvalidateRecursive(_complexGrid);
            _complexGridRoot.LayoutManager.ExecuteLayoutPass();
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
