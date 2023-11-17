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
        public void IsSelector_NoMatch()
        {
            _isCalendarSelector.Match(_notMatchingControl);
        }

        [Benchmark]
        public void IsSelector_Match()
        {
            _isCalendarSelector.Match(_matchingControl);
        }

        [Benchmark]
        public void ClassSelector_NoMatch()
        {
            _classSelector.Match(_notMatchingControl);
        }

        [Benchmark]
        public void ClassSelector_Match()
        {
            _classSelector.Match(_matchingControl);
        }

        [Benchmark]
        public void OrSelector_One_Match()
        {
            _orSelectorTwo.Match(_matchingControl);
        }

        [Benchmark]
        public void OrSelector_Five_Match()
        {
            _orSelectorFive.Match(_matchingControl);
        }
    }

    internal class AlwaysMatchSelector : Selector
    {
        internal override bool InTemplate => false;

        internal override bool IsCombinator => false;

        internal override Type TargetType => null;

        public override string ToString(Style owner)
        {
            return "Always";
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle parent, bool subscribe)
        {
            return SelectorMatch.AlwaysThisType;
        }

        private protected override Selector MovePrevious() => null;

        private protected override Selector MovePreviousOrParent() => null;
    }
}
