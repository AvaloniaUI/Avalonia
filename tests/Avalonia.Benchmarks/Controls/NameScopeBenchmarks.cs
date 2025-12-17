using System;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Controls
{
    [MemoryDiagnoser]
    public class NameScopeBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private StackPanel? _panel;
        private readonly Control[] _namedControls = new Control[50];
        private readonly Control[] _unnamedControls = new Control[50];

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            _panel = new StackPanel();
            
            // Create named controls
            for (int i = 0; i < _namedControls.Length; i++)
            {
                _namedControls[i] = new Border { Name = $"NamedControl{i}" };
                _panel.Children.Add(_namedControls[i]);
            }

            // Create unnamed controls
            for (int i = 0; i < _unnamedControls.Length; i++)
            {
                _unnamedControls[i] = new Border();
                _panel.Children.Add(_unnamedControls[i]);
            }

            _root.Child = _panel;
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark FindNameScope - exercises optimized foreach instead of LINQ OfType/Select/FirstOrDefault
        /// </summary>
        [Benchmark(Baseline = true)]
        public INameScope? FindNameScope()
        {
            return _namedControls[25].FindNameScope();
        }

        /// <summary>
        /// Benchmark finding named element
        /// </summary>
        [Benchmark]
        public object? FindByName()
        {
            return _root?.FindControl<Border>("NamedControl25");
        }

        /// <summary>
        /// Benchmark finding multiple named elements
        /// </summary>
        [Benchmark]
        public int FindMultipleByName()
        {
            int found = 0;
            for (int i = 0; i < 10; i++)
            {
                if (_root?.FindControl<Border>($"NamedControl{i * 5}") != null)
                {
                    found++;
                }
            }
            return found;
        }

        /// <summary>
        /// Benchmark FindNameScope from deeply nested control
        /// </summary>
        [Benchmark]
        public INameScope? FindNameScope_DeepNested()
        {
            // Find from the last named control (deeper in the visual tree)
            return _namedControls[_namedControls.Length - 1].FindNameScope();
        }

        /// <summary>
        /// Benchmark FindNameScope from unnamed control
        /// </summary>
        [Benchmark]
        public INameScope? FindNameScope_Unnamed()
        {
            return _unnamedControls[25].FindNameScope();
        }
    }
}
