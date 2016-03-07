// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Perspex.LogicalTree;

namespace Perspex.Styling
{
    internal class DescendentSelector : Selector
    {
        private readonly Selector _parent;
        private string _selectorString;

        public DescendentSelector(Selector parent)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Descendent selector must be preceeded by a selector.");
            }

            _parent = parent;
        }

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
            List<IObservable<bool>> descendentMatches = new List<IObservable<bool>>();

            while (c != null)
            {
                c = c.LogicalParent;

                if (c is IStyleable)
                {
                    var match = _parent.Match((IStyleable)c, subscribe);

                    if (match.ImmediateResult != null)
                    {
                        if (match.ImmediateResult == true)
                        {
                            return SelectorMatch.True;
                        }
                    }
                    else
                    {
                        descendentMatches.Add(match.ObservableResult);
                    }
                }
            }

            if (descendentMatches.Count > 0)
            {
                return new SelectorMatch(StyleActivator.Or(descendentMatches));
            }
            else
            {
                return SelectorMatch.False;
            }
        }

        protected override Selector MovePrevious() => null;
    }
}
