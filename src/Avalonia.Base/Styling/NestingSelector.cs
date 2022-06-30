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
            if (parent is Style s && s.Selector is not null)
            {
                return SelectorMatch.AlwaysThisType;
            }
            else if (parent is ControlTheme theme)
            {
                if (theme.TargetType is null)
                    throw new InvalidOperationException("ControlTheme has no TargetType.");
                return theme.TargetType.IsAssignableFrom(control.StyleKey) ?
                    SelectorMatch.AlwaysThisType :
                    SelectorMatch.NeverThisType;
            }

            throw new InvalidOperationException(
                "Nesting selector was specified but cannot determine parent selector.");
        }

        private protected override (Selector?, IStyle?) MovePrevious(IStyle? parent)
        {
            return parent is Style parentStyle ? (parentStyle.Selector, parentStyle.Parent) : (null, null);
        }

        private protected override Selector? MovePreviousOrParent() => null;
    }
}
