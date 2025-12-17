using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class StyleActivatorBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private Control? _control;

        [Params(1, 3, 5)]
        public int ClassCount { get; set; }

        [Params(1, 5, 10)]
        public int StyleCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            // Add styles that match on classes
            for (int i = 0; i < StyleCount; i++)
            {
                var className = $"class{i % ClassCount}";
                _root.Styles.Add(new Style(x => x.OfType<Border>().Class(className))
                {
                    Setters =
                    {
                        new Setter(Border.BackgroundProperty, Avalonia.Media.Brushes.Red)
                    }
                });
            }

            _control = new Border { Width = 100, Height = 100 };
            _root.Child = _control;
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark adding a single class
        /// </summary>
        [Benchmark(Baseline = true)]
        public void AddSingleClass()
        {
            _control!.Classes.Add("class0");
            _control.Classes.Remove("class0");
        }

        /// <summary>
        /// Benchmark toggling a class that triggers style activation
        /// </summary>
        [Benchmark]
        public void ToggleClassWithStyle()
        {
            for (int i = 0; i < ClassCount; i++)
            {
                _control!.Classes.Add($"class{i}");
            }
            for (int i = 0; i < ClassCount; i++)
            {
                _control!.Classes.Remove($"class{i}");
            }
        }

        /// <summary>
        /// Benchmark pseudoclass changes (like :pointerover)
        /// </summary>
        [Benchmark]
        public void TogglePseudoClass()
        {
            ((IPseudoClasses)_control!.Classes).Add(":pointerover");
            ((IPseudoClasses)_control.Classes).Remove(":pointerover");
        }

        /// <summary>
        /// Benchmark class matching with multiple classes
        /// </summary>
        [Benchmark]
        public void MatchMultipleClasses()
        {
            // Add multiple classes at once
            _control!.Classes.AddRange(new[] { "class0", "class1", "class2" });
            _control.Classes.Clear();
        }
    }
}
