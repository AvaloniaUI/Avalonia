using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Input
{
    [MemoryDiagnoser]
    public class EventRoutingBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Control? _deepestControl;
        private int _handlerCallCount;

        [Params(5, 10, 20)]
        public int NestingDepth { get; set; }

        [Params(0, 1, 3)]
        public int HandlersPerLevel { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };
            _handlerCallCount = 0;

            // Create nested control structure
            Control currentControl = new Border { Width = 1000, Height = 1000 };
            _root.Child = currentControl;

            if (HandlersPerLevel > 0)
            {
                AttachHandlers((Border)currentControl);
            }

            for (int depth = 0; depth < NestingDepth; depth++)
            {
                var child = new Border
                {
                    Width = 900 - (depth * 30),
                    Height = 900 - (depth * 30)
                };

                if (HandlersPerLevel > 0)
                {
                    AttachHandlers(child);
                }

                ((Border)currentControl).Child = child;
                currentControl = child;
            }

            _deepestControl = currentControl;
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private void AttachHandlers(Control control)
        {
            for (int i = 0; i < HandlersPerLevel; i++)
            {
                control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
                control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _handlerCallCount++;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark raising a bubbling event from deepest control
        /// </summary>
        [Benchmark]
        public void RaiseBubblingEvent()
        {
            var args = new RoutedEventArgs(Button.ClickEvent, _deepestControl);
            _deepestControl!.RaiseEvent(args);
        }

        /// <summary>
        /// Benchmark raising a tunneling+bubbling event (like pointer events)
        /// </summary>
        [Benchmark]
        public void RaiseTunnelAndBubbleEvent()
        {
            _handlerCallCount = 0;
            var args = new PointerPressedEventArgs(
                _deepestControl,
                new Pointer(1, PointerType.Mouse, true),
                _root,
                new Point(500, 500),
                (ulong)Environment.TickCount,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None);

            _deepestControl!.RaiseEvent(args);
        }

        /// <summary>
        /// Benchmark building an event route
        /// </summary>
        [Benchmark]
        public int BuildEventRoute()
        {
            using var route = new EventRoute(Button.ClickEvent);
            var current = _deepestControl;

            while (current != null)
            {
                if (current is Interactive interactive)
                {
                    route.AddClassHandler(interactive);
                }
                current = current.Parent as Control;
            }

            return route.HasHandlers ? 1 : 0;
        }

        /// <summary>
        /// Benchmark raising a direct event (no routing)
        /// </summary>
        [Benchmark(Baseline = true)]
        public void RaiseDirectEvent()
        {
            var args = new RoutedEventArgs(Control.LoadedEvent, _deepestControl);
            _deepestControl!.RaiseEvent(args);
        }
    }
}
