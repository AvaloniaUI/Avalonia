// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Styling
{
    internal class TemplateSelector : Selector
    {
        private readonly Selector _parent;

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
        public override Type TargetType => null;

        public override string ToString()
        {
            return _parent.ToString() + " /template/ ";
        }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            IStyleable templatedParent = control.TemplatedParent as IStyleable;

            if (templatedParent == null)
            {
                throw new InvalidOperationException(
                    "Cannot call Template selector on control with null TemplatedParent.");
            }

            return _parent.Match(templatedParent, subscribe) ?? SelectorMatch.True;
        }

        protected override Selector MovePrevious() => null;
    }
}
