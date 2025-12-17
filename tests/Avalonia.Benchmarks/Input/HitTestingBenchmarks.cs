using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Input
{
    [MemoryDiagnoser]
    public class HitTestingBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private readonly List<Control> _controls = new();
        private Point _centerPoint;
        private Point _cornerPoint;

        [Params(10, 50, 100)]
        public int ControlCount { get; set; }

        [Params(5, 10)]
        public int NestingDepth { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };
            _controls.Clear();

            // Create nested visual tree structure
            Panel currentParent = new Canvas { Width = 1000, Height = 1000 };
            _root.Child = currentParent;

            for (int depth = 0; depth < NestingDepth; depth++)
            {
                var panel = new Canvas
                {
                    Width = 900 - (depth * 50),
                    Height = 900 - (depth * 50)
                };
                Canvas.SetLeft(panel, 25);
                Canvas.SetTop(panel, 25);
                currentParent.Children.Add(panel);
                currentParent = panel;
            }

            // Add controls at the deepest level
            var controlSize = Math.Max(10, (800 - NestingDepth * 50) / (int)Math.Sqrt(ControlCount));
            var gridSize = (int)Math.Ceiling(Math.Sqrt(ControlCount));

            for (int i = 0; i < ControlCount; i++)
            {
                var row = i / gridSize;
                var col = i % gridSize;
                var control = new Border
                {
                    Width = controlSize - 2,
                    Height = controlSize - 2,
                    Background = Avalonia.Media.Brushes.Blue
                };
                Canvas.SetLeft(control, col * controlSize);
                Canvas.SetTop(control, row * controlSize);
                currentParent.Children.Add(control);
                _controls.Add(control);
            }

            _root.LayoutManager.ExecuteInitialLayoutPass();

            // Calculate test points
            _centerPoint = new Point(500, 500);
            _cornerPoint = new Point(10, 10);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark single hit test at center (hits deepest nested control)
        /// </summary>
        [Benchmark]
        public IInputElement? HitTestCenter()
        {
            return _root!.InputHitTest(_centerPoint);
        }

        /// <summary>
        /// Benchmark single hit test at corner (may miss controls)
        /// </summary>
        [Benchmark]
        public IInputElement? HitTestCorner()
        {
            return _root!.InputHitTest(_cornerPoint);
        }

        /// <summary>
        /// Benchmark getting all elements at a point
        /// </summary>
        [Benchmark]
        public int HitTestAllAtPoint()
        {
            int count = 0;
            foreach (var element in _root!.GetInputElementsAt(_centerPoint))
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Benchmark multiple hit tests simulating mouse movement
        /// </summary>
        [Benchmark]
        public int SimulateMouseMovement()
        {
            int hitCount = 0;
            // Simulate diagonal mouse movement across the control
            for (int i = 0; i < 100; i++)
            {
                var point = new Point(100 + i * 8, 100 + i * 8);
                if (_root!.InputHitTest(point) != null)
                {
                    hitCount++;
                }
            }
            return hitCount;
        }

        /// <summary>
        /// Benchmark TransformToVisual which is used during hit testing
        /// </summary>
        [Benchmark]
        public Matrix? TransformToVisual_DeepNesting()
        {
            if (_controls.Count > 0)
            {
                return _controls[0].TransformToVisual(_root);
            }
            return null;
        }

        /// <summary>
        /// Benchmark GetVisualAt with custom filter
        /// </summary>
        [Benchmark]
        public Visual? HitTestWithFilter()
        {
            return ((Visual)_root!).GetVisualAt(_centerPoint, v => v is Border);
        }
    }
}
