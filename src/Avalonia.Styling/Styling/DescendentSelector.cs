// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.LogicalTree;

namespace Avalonia.Styling
{
    internal class DescendantSelector : Selector
    {
        private readonly Selector _parent;
        private string _selectorString;

        public DescendantSelector(Selector parent)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Descendant selector must be preceeded by a selector.");
            }

            _parent = parent;
        }

        /// <inheritdoc/>
        public override bool IsCombinator => true;

        /// <inheritdoc/>
        public override bool InTemplate => _parent.InTemplate;

        /// <inheritdoc/>
        public override Type TargetType => null;

        public override string ToString()
        {
            if (_selectorString == null)
            {
                _selectorString = _parent.ToString() + ' ';
            }

            return _selectorString;
        }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            ILogical c = (ILogical)control;
            List<IObservable<bool>> descendantMatches = new List<IObservable<bool>>();

            while (c != null)
            {
                c = c.LogicalParent;

                if (c is IStyleable)
                {
                    var match = _parent.Match((IStyleable)c, subscribe);

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
                return new SelectorMatch(StyleActivator.Or(descendantMatches));
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }
        }

        protected override Selector MovePrevious() => null;
    }
}
