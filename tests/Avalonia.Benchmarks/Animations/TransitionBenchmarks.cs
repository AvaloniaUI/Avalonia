using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Animations
{
    [MemoryDiagnoser]
    public class TransitionBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Border? _control;
        private Transitions? _transitions1;
        private Transitions? _transitions2;
        private Transitions? _transitions3;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            _control = new Border { Width = 100, Height = 100 };
            _root.Child = _control;

            // Create different transition sets for testing
            _transitions1 = new Transitions
            {
                new DoubleTransition { Property = Border.OpacityProperty, Duration = TimeSpan.FromMilliseconds(100) },
                new DoubleTransition { Property = Border.WidthProperty, Duration = TimeSpan.FromMilliseconds(100) },
            };

            _transitions2 = new Transitions
            {
                new DoubleTransition { Property = Border.OpacityProperty, Duration = TimeSpan.FromMilliseconds(100) },
                new DoubleTransition { Property = Border.HeightProperty, Duration = TimeSpan.FromMilliseconds(100) },
            };

            _transitions3 = new Transitions
            {
                new DoubleTransition { Property = Border.OpacityProperty, Duration = TimeSpan.FromMilliseconds(100) },
                new DoubleTransition { Property = Border.WidthProperty, Duration = TimeSpan.FromMilliseconds(100) },
                new DoubleTransition { Property = Border.HeightProperty, Duration = TimeSpan.FromMilliseconds(100) },
                new BrushTransition { Property = Border.BackgroundProperty, Duration = TimeSpan.FromMilliseconds(100) },
            };

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark setting transitions for the first time
        /// </summary>
        [Benchmark(Baseline = true)]
        public void SetTransitions_Initial()
        {
            _control!.Transitions = null;
            _control.Transitions = _transitions1;
        }

        /// <summary>
        /// Benchmark changing transitions - exercises the optimized HashSet-based Except implementation
        /// </summary>
        [Benchmark]
        public void SetTransitions_Change()
        {
            _control!.Transitions = _transitions1;
            _control.Transitions = _transitions2;
        }

        /// <summary>
        /// Benchmark setting same transitions (no change)
        /// </summary>
        [Benchmark]
        public void SetTransitions_NoChange()
        {
            _control!.Transitions = _transitions1;
            _control.Transitions = _transitions1;
        }

        /// <summary>
        /// Benchmark clearing transitions
        /// </summary>
        [Benchmark]
        public void SetTransitions_Clear()
        {
            _control!.Transitions = _transitions1;
            _control.Transitions = null;
        }

        /// <summary>
        /// Benchmark changing to larger transition set
        /// </summary>
        [Benchmark]
        public void SetTransitions_ToLarger()
        {
            _control!.Transitions = _transitions1;
            _control.Transitions = _transitions3;
        }

        /// <summary>
        /// Benchmark rapid transition changes - stresses the diffing algorithm
        /// </summary>
        [Benchmark]
        public void SetTransitions_RapidChanges()
        {
            for (int i = 0; i < 10; i++)
            {
                _control!.Transitions = (i % 2 == 0) ? _transitions1 : _transitions2;
            }
        }
    }
}
