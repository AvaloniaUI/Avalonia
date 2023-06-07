using System;

namespace Avalonia.Styling
{
    internal class TemplateSelector : Selector
    {
        private readonly Selector _parent;
        private string? _selectorString;

        public TemplateSelector(Selector parent)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Template selector must be preceeded by a selector.");
            }

            _parent = parent;
        }

        /// <inheritdoc/>
        internal override bool InTemplate => true;

        /// <inheritdoc/>
        internal override bool IsCombinator => true;

        /// <inheritdoc/>
        internal override Type? TargetType => null;

        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString(owner) + " /template/ ";
            }

            return _selectorString;
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            var templatedParent = control.TemplatedParent as StyledElement;

            if (templatedParent == null)
            {
                return SelectorMatch.NeverThisInstance;
            }

            return _parent.Match(templatedParent, parent, subscribe);
        }

        private protected override Selector? MovePrevious() => null;
        private protected override Selector? MovePreviousOrParent() => _parent;
    }
}
