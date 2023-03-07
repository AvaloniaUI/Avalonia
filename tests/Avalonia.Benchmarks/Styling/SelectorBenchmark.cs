using System;
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
        private readonly Selector _orSelectorTwo;
        private readonly Selector _orSelectorFive;

        public SelectorBenchmark()
        {
            _notMatchingControl = new Control();
            _matchingControl = new Calendar();

            const string className = "selector-class";

            _matchingControl.Classes.Add(className);

            _isCalendarSelector = Selectors.Is<Calendar>(null);
            _classSelector = Selectors.Class(null, className);

            _orSelectorTwo = Selectors.Or(new AlwaysMatchSelector(), new AlwaysMatchSelector());
            _orSelectorFive = Selectors.Or(
                new AlwaysMatchSelector(), 
                new AlwaysMatchSelector(),
                new AlwaysMatchSelector(),
                new AlwaysMatchSelector(),
                new AlwaysMatchSelector());
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

        [Benchmark]
        public SelectorMatch OrSelector_One_Match()
        {
            return _orSelectorTwo.Match(_matchingControl);
        }

        [Benchmark]
        public SelectorMatch OrSelector_Five_Match()
        {
            return _orSelectorFive.Match(_matchingControl);
        }
    }

    internal class AlwaysMatchSelector : Selector
    {
        public override bool InTemplate => false;

        public override bool IsCombinator => false;

        public override Type TargetType => null;

        public override string ToString(Style owner)
        {
            return "Always";
        }

        protected override SelectorMatch Evaluate(StyledElement control, IStyle parent, bool subscribe)
        {
            return SelectorMatch.AlwaysThisType;
        }

        protected override Selector MovePrevious() => null;

        protected override Selector MovePreviousOrParent() => null;
    }
}
