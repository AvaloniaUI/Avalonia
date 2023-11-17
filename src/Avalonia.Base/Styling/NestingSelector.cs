using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// The `^` nesting style selector.
    /// </summary>
    internal class NestingSelector : Selector
    {
        internal override bool InTemplate => false;
        internal override bool IsCombinator => false;
        internal override Type? TargetType => null;

        public override string ToString(Style? owner) => owner?.Parent?.ToString() ?? "^";

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (parent is Style s && s.Selector is not null)
            {
                return s.Selector.Match(control, s.Parent, subscribe);
            }
            else if (parent is ControlTheme theme)
            {
                if (theme.TargetType is null)
                    throw new InvalidOperationException("ControlTheme has no TargetType.");
                return theme.TargetType.IsAssignableFrom(StyledElement.GetStyleKey(control)) ?
                    SelectorMatch.AlwaysThisType :
                    SelectorMatch.NeverThisType;
            }

            throw new InvalidOperationException(
                "Nesting selector was specified but cannot determine parent selector.");
        }

        private protected override Selector? MovePrevious() => null;
        private protected override Selector? MovePreviousOrParent() => null;
    }
}
