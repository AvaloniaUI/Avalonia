// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Primitives;

namespace Perspex.Controls.UnitTests
{
    internal class TestTemplatedControl : TemplatedControl
    {
        public bool OnTemplateAppliedCalled { get; private set; }

        public void AddVisualChild(IVisual visual)
        {
            VisualChildren.Add(visual);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            OnTemplateAppliedCalled = true;
        }
    }
}
