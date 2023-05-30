using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    internal class DescendantSelector : Selector
    {
        private readonly Selector _parent;
        private string? _selectorString;

        public DescendantSelector(Selector? parent)
        {
            _parent = parent ?? throw new InvalidOperationException("Descendant selector must be preceded by a selector.");
        }

        /// <inheritdoc/>
        internal override bool IsCombinator => true;

        /// <inheritdoc/>
        internal override bool InTemplate => _parent.InTemplate;

        /// <inheritdoc/>
        internal override Type? TargetType => null;

        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString(owner) + ' ';
            }

            return _selectorString;
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            var c = (ILogical)control;
            var descendantMatches = new OrActivatorBuilder();

            while (c != null)
            {
                c = c.LogicalParent;

                if (c is StyledElement s)
                {
                    var match = _parent.Match(s, parent, subscribe);

                    if (match.Result == SelectorMatchResult.Sometimes)
                    {
                        descendantMatches.Add(match.Activator);
                    }
                    else if (match.IsMatch)
                    {
                        return SelectorMatch.AlwaysThisInstance;
                    }
                }
            }

            if (descendantMatches.Count > 0)
            {
                return new SelectorMatch(descendantMatches.Get());
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }
        }

        private protected override Selector? MovePrevious() => null;
        private protected override Selector? MovePreviousOrParent() => _parent;
    }
}
