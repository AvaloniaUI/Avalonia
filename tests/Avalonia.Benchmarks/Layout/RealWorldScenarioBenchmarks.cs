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
    /// Benchmarks for real-world UI scenarios.
    /// Tests common layout patterns found in actual applications.
    /// </summary>
    [MemoryDiagnoser]
    public class RealWorldScenarioBenchmarks : IDisposable
    {
        private IDisposable _app = null!;
        private TestRoot _formRoot = null!;
        private TestRoot _dashboardRoot = null!;
        private TestRoot _dataGridLikeRoot = null!;
        private Control _form = null!;
        private Control _dashboard = null!;
        private Control _dataGridLike = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Form layout: Labels + TextBoxes in a Grid
            _formRoot = new TestRoot { Renderer = new NullRenderer() };
            _form = CreateFormLayout(20);
            _formRoot.Child = _form;
            _formRoot.LayoutManager.ExecuteInitialLayoutPass();

            // Dashboard layout: Multiple panels with various content
            _dashboardRoot = new TestRoot { Renderer = new NullRenderer() };
            _dashboard = CreateDashboardLayout();
            _dashboardRoot.Child = _dashboard;
            _dashboardRoot.LayoutManager.ExecuteInitialLayoutPass();

            // DataGrid-like layout: Many rows with fixed columns
            _dataGridLikeRoot = new TestRoot { Renderer = new NullRenderer() };
            _dataGridLike = CreateDataGridLikeLayout(100, 10);
            _dataGridLikeRoot.Child = _dataGridLike;
            _dataGridLikeRoot.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static Control CreateFormLayout(int fieldCount)
        {
            var grid = new Grid();
            
            // Two columns: labels and inputs
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

            for (var i = 0; i < fieldCount; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                var label = new TextBlock { Text = $"Field {i}:", Margin = new Thickness(5) };
                Grid.SetRow(label, i);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                var input = new TextBox { Text = $"Value {i}", Margin = new Thickness(5) };
                Grid.SetRow(input, i);
                Grid.SetColumn(input, 1);
                grid.Children.Add(input);
            }

            return new Border
            {
                Padding = new Thickness(10),
                Child = grid
            };
        }

        private static Control CreateDashboardLayout()
        {
            var mainGrid = new Grid();
            
            // 3x3 dashboard grid
            for (var i = 0; i < 3; i++)
            {
                mainGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            }

            // Add dashboard panels
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var panel = CreateDashboardPanel($"Panel {row * 3 + col + 1}");
                    Grid.SetRow(panel, row);
                    Grid.SetColumn(panel, col);
                    mainGrid.Children.Add(panel);
                }
            }

            return mainGrid;
        }

        private static Control CreateDashboardPanel(string title)
        {
            var panel = new Border
            {
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = title, FontSize = 16 },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                            {
                                new Button { Content = "Action 1", Margin = new Thickness(2) },
                                new Button { Content = "Action 2", Margin = new Thickness(2) },
                            }
                        },
                        new TextBlock { Text = "Content area..." },
                        new ProgressBar { Minimum = 0, Maximum = 100, Value = 50, Height = 10 }
                    }
                }
            };
            return panel;
        }

        private static Control CreateDataGridLikeLayout(int rowCount, int columnCount)
        {
            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };

            var grid = new Grid();

            // Header row + data rows
            grid.RowDefinitions.Add(new RowDefinition(30, GridUnitType.Pixel)); // Header
            for (var i = 0; i < rowCount; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition(25, GridUnitType.Pixel));
            }

            // Columns
            for (var i = 0; i < columnCount; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(100, GridUnitType.Pixel));
            }

            // Header cells
            for (var col = 0; col < columnCount; col++)
            {
                var header = new Border
                {
                    Child = new TextBlock { Text = $"Column {col}", FontWeight = Avalonia.Media.FontWeight.Bold }
                };
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, col);
                grid.Children.Add(header);
            }

            // Data cells
            for (var row = 0; row < rowCount; row++)
            {
                for (var col = 0; col < columnCount; col++)
                {
                    var cell = new Border
                    {
                        Child = new TextBlock { Text = $"R{row}C{col}" }
                    };
                    Grid.SetRow(cell, row + 1);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }

            scrollViewer.Content = grid;
            return scrollViewer;
        }

        /// <summary>
        /// Form layout with label-input pairs.
        /// Common in data entry applications.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FormLayout_FullLayout()
        {
            InvalidateRecursive(_form);
            _formRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Dashboard with multiple panels.
        /// Common in monitoring/admin applications.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DashboardLayout_FullLayout()
        {
            InvalidateRecursive(_dashboard);
            _dashboardRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// DataGrid-like layout with many cells.
        /// Common in data-heavy applications.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DataGridLikeLayout_FullLayout()
        {
            InvalidateRecursive(_dataGridLike);
            _dataGridLikeRoot.LayoutManager.ExecuteLayoutPass();
        }

        /// <summary>
        /// Form with single field update.
        /// Tests localized change handling.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FormLayout_SingleFieldUpdate()
        {
            if (_form is Border border && border.Child is Grid grid && grid.Children.Count > 1)
            {
                if (grid.Children[1] is TextBox textBox)
                {
                    textBox.Text = "Updated Value";
                    _formRoot.LayoutManager.ExecuteLayoutPass();
                    textBox.Text = "Value 0";
                    _formRoot.LayoutManager.ExecuteLayoutPass();
                }
            }
        }

        /// <summary>
        /// Dashboard with panel resize.
        /// Tests star sizing recalculation.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DashboardLayout_PanelResize()
        {
            if (_dashboard is Grid grid && grid.Children.Count > 0 && grid.Children[0] is Border panel)
            {
                panel.Width = 300;
                _dashboardRoot.LayoutManager.ExecuteLayoutPass();
                panel.Width = double.NaN;
                _dashboardRoot.LayoutManager.ExecuteLayoutPass();
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
            else if (control is Decorator decorator && decorator.Child != null)
            {
                InvalidateRecursive(decorator.Child);
            }
            else if (control is ContentControl cc && cc.Content is Control content)
            {
                InvalidateRecursive(content);
            }
            else if (control is ScrollViewer sv && sv.Content is Control svContent)
            {
                InvalidateRecursive(svContent);
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
