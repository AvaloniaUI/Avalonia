using Avalonia.Controls;
using Avalonia.Styling;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class SelectorBenchmark
    {
        private readonly Control _notMatchingControl;
        private readonly Calendar _matchingControl;
        private readonly Selector _isCalendarSelector;
        private readonly Selector _classSelector;

        public SelectorBenchmark()
        {
            _notMatchingControl = new Control();
            _matchingControl = new Calendar();

            const string className = "selector-class";

            _matchingControl.Classes.Add(className);

            _isCalendarSelector = Selectors.Is<Calendar>(null);
            _classSelector = Selectors.Class(null, className);
        }

        [Benchmark]
        public SelectorMatch IsSelector_NoMatch()
        {
            return _isCalendarSelector.Match(_notMatchingControl);
        }

        [Benchmark]
        public SelectorMatch IsSelector_Match()
        {
            return _isCalendarSelector.Match(_matchingControl);
        }

        [Benchmark]
        public SelectorMatch ClassSelector_NoMatch()
        {
            return _classSelector.Match(_notMatchingControl);
        }

        [Benchmark]
        public SelectorMatch ClassSelector_Match()
        {
            return _classSelector.Match(_matchingControl);
        }
    }
}
