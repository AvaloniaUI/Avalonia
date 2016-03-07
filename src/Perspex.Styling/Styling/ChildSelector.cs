// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.LogicalTree;

namespace Perspex.Styling
{
    internal class ChildSelector : Selector
    {
        private readonly Selector _parent;

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
            return _parent.ToString() + " > ";
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
