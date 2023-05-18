using System;
using Avalonia.LogicalTree;

namespace Avalonia.Styling
{
    internal class ChildSelector : Selector
    {
        private readonly Selector _parent;
        private string? _selectorString;

        public ChildSelector(Selector parent)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Child selector must be preceeded by a selector.");
            }

            _parent = parent;
        }

        /// <inheritdoc/>
        internal override bool InTemplate => _parent.InTemplate;

        /// <inheritdoc/>
        internal override bool IsCombinator => true;

        /// <inheritdoc/>
        internal override Type? TargetType => null;

        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString(owner) + " > ";
            }

            return _selectorString;
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            var controlParent = ((ILogical)control).LogicalParent;

            if (controlParent != null)
            {
                var parentMatch = _parent.Match((StyledElement)controlParent, parent, subscribe);

                if (parentMatch.Result == SelectorMatchResult.Sometimes)
                {
                    return parentMatch;
                }
                else if (parentMatch.IsMatch)
                {
                    return SelectorMatch.AlwaysThisInstance;
                }
                else
                {
                    return SelectorMatch.NeverThisInstance;
                }
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
