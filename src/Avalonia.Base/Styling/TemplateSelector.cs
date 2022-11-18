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
        public override bool InTemplate => true;

        /// <inheritdoc/>
        public override bool IsCombinator => true;

        /// <inheritdoc/>
        public override Type? TargetType => null;

        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString(owner) + " /template/ ";
            }

            return _selectorString;
        }

        protected override SelectorMatch Evaluate(IStyleable control, IStyle? parent, bool subscribe)
        {
            var templatedParent = control.TemplatedParent as IStyleable;

            if (templatedParent == null)
            {
                return SelectorMatch.NeverThisInstance;
            }

            return _parent.Match(templatedParent, parent, subscribe);
        }

        protected override Selector? MovePrevious() => null;
        protected override Selector? MovePreviousOrParent() => _parent;
    }
}
