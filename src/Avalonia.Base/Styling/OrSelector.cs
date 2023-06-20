using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// The OR style selector.
    /// </summary>
    internal sealed class OrSelector : Selector
    {
        private readonly IReadOnlyList<Selector> _selectors;
        private string? _selectorString;
        private Type? _targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrSelector"/> class.
        /// </summary>
        /// <param name="selectors">The selectors to OR.</param>
        public OrSelector(IReadOnlyList<Selector> selectors)
        {
            if (selectors is null)
            {
                throw new ArgumentNullException(nameof(selectors));
            }

            if (selectors.Count <= 1)
            {
                throw new ArgumentException("Need more than one selector to OR.");
            }

            _selectors = selectors;
        }

        /// <inheritdoc/>
        internal override bool InTemplate => false;

        /// <inheritdoc/>
        internal override bool IsCombinator => false;

        /// <inheritdoc/>
        internal override Type? TargetType => _targetType ??= EvaluateTargetType();

        /// <inheritdoc/>
        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                _selectorString = string.Join(", ", _selectors.Select(x => x.ToString(owner)));
            }

            return _selectorString;
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            var activators = new OrActivatorBuilder();
            var neverThisInstance = false;

            var count = _selectors.Count;

            for (var i = 0; i < count; i++)
            {
                var match = _selectors[i].Match(control, parent, subscribe);

                switch (match.Result)
                {
                    case SelectorMatchResult.AlwaysThisType:
                    case SelectorMatchResult.AlwaysThisInstance:
                        return match;
                    case SelectorMatchResult.NeverThisInstance:
                        neverThisInstance = true;
                        break;
                    case SelectorMatchResult.Sometimes:
                        activators.Add(match.Activator!);
                        break;
                }
            }

            if (activators.Count > 0)
            {
                return new SelectorMatch(activators.Get());
            }
            else if (neverThisInstance)
            {
                return SelectorMatch.NeverThisInstance;
            }
            else
            {
                return SelectorMatch.NeverThisType;
            }
        }

        private protected override Selector? MovePrevious() => null;
        private protected override Selector? MovePreviousOrParent() => null;

        internal override void ValidateNestingSelector(bool inControlTheme)
        {
            var count = _selectors.Count;

            for (var i = 0; i < count; i++)
            {
                _selectors[i].ValidateNestingSelector(inControlTheme);
            }
        }

        private Type? EvaluateTargetType()
        {
            Type? result = null;

            var count = _selectors.Count;

            for (var i = 0; i < count; i++)
            {
                var selector = _selectors[i];
                if (selector.TargetType == null)
                {
                    return null;
                }
                else if (result == null)
                {
                    result = selector.TargetType;
                }
                else
                {
                    while (result is not null && !result.IsAssignableFrom(selector.TargetType))
                    {
                        result = result.BaseType;
                    }
                }
            }

            return result;
        }
    }
}

