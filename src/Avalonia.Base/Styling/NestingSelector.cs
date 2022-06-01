using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// The `^` nesting style selector.
    /// </summary>
    internal class NestingSelector : Selector
    {
        public override bool InTemplate => false;
        public override bool IsCombinator => false;
        public override Type? TargetType => null;

        public override string ToString() => "^";

        protected override SelectorMatch Evaluate(IStyleable control, IStyle? parent, bool subscribe)
        {
            if (parent is StyleBase s && s.HasSelector)
            {
                return s.Match(control, null, subscribe);
            }

            throw new InvalidOperationException(
                "Nesting selector was specified but cannot determine parent selector.");
        }

        protected override Selector? MovePrevious() => null;
        internal override bool HasValidNestingSelector() => true;
    }
}
