// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.LogicalTree;

namespace Avalonia.Styling
{
    internal class ChildSelector : Selector
    {
        private readonly Selector _parent;
        private string _selectorString;

        public ChildSelector(Selector parent)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Child selector must be preceeded by a selector.");
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
                _selectorString = _parent.ToString() + " > ";
            }

            return _selectorString;
        }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            var controlParent = ((ILogical)control).LogicalParent;

            if (controlParent != null)
            {
                return _parent.Match((IStyleable)controlParent, subscribe);
            }
            else
            {
                return SelectorMatch.False;
            }
        }

        protected override Selector MovePrevious() => null;
    }
}
