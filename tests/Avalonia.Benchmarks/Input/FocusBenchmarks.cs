using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Input
{
    [MemoryDiagnoser]
    public class FocusBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private readonly List<Button> _focusableControls = new();
        private Button? _firstButton;
        private Button? _lastButton;

        [Params(10, 50, 100)]
        public int FocusableControlCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };
            _focusableControls.Clear();

            var stackPanel = new StackPanel();
            _root.Child = stackPanel;

            // Create many focusable controls
            for (int i = 0; i < FocusableControlCount; i++)
            {
                var button = new Button
                {
                    Content = $"Button {i}",
                    Width = 100,
                    Height = 30,
                    Focusable = true,
                    TabIndex = i
                };
                stackPanel.Children.Add(button);
                _focusableControls.Add(button);
            }

            _firstButton = _focusableControls[0];
            _lastButton = _focusableControls[_focusableControls.Count - 1];

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark focusing a control
        /// </summary>
        [Benchmark]
        public bool FocusSingleControl()
        {
            return _firstButton!.Focus();
        }

        /// <summary>
        /// Benchmark changing focus between two controls
        /// </summary>
        [Benchmark]
        public void ChangeFocusBetweenControls()
        {
            _firstButton!.Focus();
            _lastButton!.Focus();
        }

        /// <summary>
        /// Benchmark cycling through all focusable controls
        /// </summary>
        [Benchmark]
        public int CycleThroughAllFocusableControls()
        {
            int focusCount = 0;
            foreach (var control in _focusableControls)
            {
                if (control.Focus())
                {
                    focusCount++;
                }
            }
            return focusCount;
        }

        /// <summary>
        /// Benchmark Tab navigation (Next focus)
        /// </summary>
        [Benchmark]
        public void TabNavigationForward()
        {
            _firstButton!.Focus();
            
            // Simulate 10 tab key presses
            for (int i = 0; i < 10; i++)
            {
                KeyboardNavigationHandler.GetNext(_focusableControls[i % _focusableControls.Count], 
                    NavigationDirection.Next);
            }
        }

        /// <summary>
        /// Benchmark getting the focused element
        /// </summary>
        [Benchmark]
        public IInputElement? GetFocusedElement()
        {
            return FocusManager.GetFocusManager(_firstButton)?.GetFocusedElement();
        }

        /// <summary>
        /// Benchmark focus scope operations
        /// </summary>
        [Benchmark]
        public IInputElement? GetFocusScopeElement()
        {
            var focusManager = FocusManager.GetFocusManager(_firstButton);
            if (focusManager != null && _root != null)
            {
                return focusManager.GetFocusedElement(_root);
            }
            return null;
        }
    }
}
