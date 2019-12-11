// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Styling
{
    /// <summary>
    /// The OR style selector.
    /// </summary>
    internal class OrSelector : Selector
    {
        private readonly IReadOnlyList<Selector> _selectors;
        private string _selectorString;
        private Type _targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrSelector"/> class.
        /// </summary>
        /// <param name="selectors">The selectors to OR.</param>
        public OrSelector(IReadOnlyList<Selector> selectors)
        {
            Contract.Requires<ArgumentNullException>(selectors != null);
            Contract.Requires<ArgumentException>(selectors.Count > 1);

            _selectors = selectors;
        }

        /// <inheritdoc/>
        public override bool InTemplate => false;

        /// <inheritdoc/>
        public override bool IsCombinator => false;

        /// <inheritdoc/>
        public override Type TargetType
        {
            get
            {
                if (_targetType == null)
                {
                    _targetType = EvaluateTargetType();
                }

                return _targetType;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_selectorString == null)
            {
                _selectorString = string.Join(", ", _selectors);
            }

            return _selectorString;
        }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            var activators = new List<IObservable<bool>>();
            var neverThisInstance = false;

            foreach (var selector in _selectors)
            {
                var match = selector.Match(control, subscribe);

                switch (match.Result)
                {
                    case SelectorMatchResult.AlwaysThisType:
                    case SelectorMatchResult.AlwaysThisInstance:
                        return match;
                    case SelectorMatchResult.NeverThisInstance:
                        neverThisInstance = true;
                        break;
                    case SelectorMatchResult.Sometimes:
                        activators.Add(match.Activator);
                        break;
                }
            }

            if (activators.Count > 1)
            {
                return new SelectorMatch(StyleActivator.Or(activators));
            }
            else if (activators.Count == 1)
            {
                return new SelectorMatch(activators[0]);
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

        protected override Selector MovePrevious() => null;

        private Type EvaluateTargetType()
        {
            var result = default(Type);

            foreach (var selector in _selectors)
            {
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
                    while (!result.IsAssignableFrom(selector.TargetType))
                    {
                        result = result.BaseType;
                    }
                }
            }

            return result;
        }
    }
}

