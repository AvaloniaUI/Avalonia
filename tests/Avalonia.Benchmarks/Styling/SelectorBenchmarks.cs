using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class SelectorBenchmarks
    {
        private TestRoot _root = null!;
        private Button _button = null!;
        private Border _border = null!;
        private IDisposable _app = null!;

        private Selector _typeSelector = null!;
        private Selector _classSelector = null!;
        private Selector _multiClassSelector = null!;
        private Selector _descendantSelector = null!;
        private Selector _childSelector = null!;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot();
            _border = new Border();
            _button = new Button();
            
            _button.Classes.Add("primary");
            _button.Classes.Add("large");
            _button.Classes.Add("rounded");

            _border.Child = _button;
            _root.Child = _border;

            // Create selectors
            _typeSelector = Selectors.OfType(null, typeof(Button));
            _classSelector = Selectors.Class(null, "primary");
            _multiClassSelector = Selectors.Class(Selectors.Class(Selectors.Class(null, "primary"), "large"), "rounded");
            _descendantSelector = Selectors.OfType(Selectors.Descendant(Selectors.OfType(null, typeof(TestRoot))), typeof(Button));
            _childSelector = Selectors.OfType(Selectors.Child(Selectors.OfType(null, typeof(Border))), typeof(Button));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _app?.Dispose();
        }

        [Benchmark(Baseline = true)]
        public bool TypeSelector_Match()
        {
            return _typeSelector.Match(_button, null, false).IsMatch;
        }

        [Benchmark]
        public bool TypeSelector_NoMatch()
        {
            return _typeSelector.Match(_border, null, false).IsMatch;
        }

        [Benchmark]
        public bool ClassSelector_Match()
        {
            return _classSelector.Match(_button, null, false).IsMatch;
        }

        [Benchmark]
        public bool ClassSelector_NoMatch()
        {
            return _classSelector.Match(_border, null, false).IsMatch;
        }

        [Benchmark]
        public bool MultiClassSelector_Match()
        {
            return _multiClassSelector.Match(_button, null, false).IsMatch;
        }

        [Benchmark]
        public bool DescendantSelector_Match()
        {
            return _descendantSelector.Match(_button, null, false).IsMatch;
        }

        [Benchmark]
        public bool ChildSelector_Match()
        {
            return _childSelector.Match(_button, null, false).IsMatch;
        }

        [Benchmark]
        public bool ClassesContains()
        {
            return _button.Classes.Contains("primary");
        }

        [Benchmark]
        public void ClassesAddRemove()
        {
            _button.Classes.Add("temp");
            _button.Classes.Remove("temp");
        }
    }
}
