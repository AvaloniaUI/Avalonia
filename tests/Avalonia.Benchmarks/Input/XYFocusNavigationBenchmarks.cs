using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Input
{
    [MemoryDiagnoser]
    public class XYFocusNavigationBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Button?[,]? _buttons;
        private Button? _centerButton;
        private FocusManager? _focusManager;

        [Params(3, 5, 7)]
        public int GridSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            // Create a grid of focusable buttons
            var grid = new Grid();
            for (int i = 0; i < GridSize; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            }

            _buttons = new Button[GridSize, GridSize];
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    var button = new Button
                    {
                        Content = $"Button {row},{col}",
                        Width = 80,
                        Height = 30,
                        Focusable = true
                    };
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    grid.Children.Add(button);
                    _buttons[row, col] = button;
                }
            }

            _root.Child = grid;
            _root.LayoutManager.ExecuteInitialLayoutPass();

            // Focus the center button
            int center = GridSize / 2;
            _centerButton = _buttons[center, center];
            _centerButton!.Focus();
            
            _focusManager = FocusManager.GetFocusManager(_root);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark XY focus navigation to the right
        /// </summary>
        [Benchmark(Baseline = true)]
        public bool NavigateRight()
        {
            _centerButton!.Focus();
            return _focusManager!.TryMoveFocus(NavigationDirection.Right);
        }

        /// <summary>
        /// Benchmark XY focus navigation down
        /// </summary>
        [Benchmark]
        public bool NavigateDown()
        {
            _centerButton!.Focus();
            return _focusManager!.TryMoveFocus(NavigationDirection.Down);
        }

        /// <summary>
        /// Benchmark full navigation cycle (right, down, left, up)
        /// </summary>
        [Benchmark]
        public int NavigateCycle()
        {
            _centerButton!.Focus();
            int moves = 0;

            if (_focusManager!.TryMoveFocus(NavigationDirection.Right)) moves++;
            if (_focusManager.TryMoveFocus(NavigationDirection.Down)) moves++;
            if (_focusManager.TryMoveFocus(NavigationDirection.Left)) moves++;
            if (_focusManager.TryMoveFocus(NavigationDirection.Up)) moves++;

            return moves;
        }
    }
}
