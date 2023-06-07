using System;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// The `:not()` style selector.
    /// </summary>
    internal class NotSelector : Selector
    {
        private readonly Selector? _previous;
        private readonly Selector _argument;
        private string? _selectorString;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotSelector"/> class.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="argument">The selector to be not-ed.</param>
        public NotSelector(Selector? previous, Selector argument)
        {
            _previous = previous;
            _argument = argument ?? throw new InvalidOperationException("Not selector must have a selector argument.");
        }

        /// <inheritdoc/>
        internal override bool InTemplate => _argument.InTemplate;

        /// <inheritdoc/>
        internal override bool IsCombinator => false;

        /// <inheritdoc/>
        internal override Type? TargetType => _previous?.TargetType;

        /// <inheritdoc/>
        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                _selectorString = $"{_previous?.ToString(owner)}:not({_argument})";
            }

            return _selectorString;
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            var innerResult = _argument.Match(control, parent, subscribe);

            switch (innerResult.Result)
            {
                case SelectorMatchResult.AlwaysThisInstance:
                    return SelectorMatch.NeverThisInstance;
                case SelectorMatchResult.AlwaysThisType:
                    return SelectorMatch.NeverThisType;
                case SelectorMatchResult.NeverThisInstance:
                    return SelectorMatch.AlwaysThisInstance;
                case SelectorMatchResult.NeverThisType:
                    return SelectorMatch.AlwaysThisType;
                case SelectorMatchResult.Sometimes:
                    return new SelectorMatch(new NotActivator(innerResult.Activator!));
                default:
                    throw new InvalidOperationException("Invalid SelectorMatchResult.");
            }
        }

        private protected override Selector? MovePrevious() => _previous;
        private protected override Selector? MovePreviousOrParent() => _previous;
    }
}
