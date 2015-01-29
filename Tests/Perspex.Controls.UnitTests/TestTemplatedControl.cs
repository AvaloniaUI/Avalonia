// -----------------------------------------------------------------------
// <copyright file="TestRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using Perspex.Controls.Primitives;

    internal class TestTemplatedControl : TemplatedControl
    {
        public bool OnTemplateAppliedCalled { get; private set; }

        protected override void OnTemplateApplied()
        {
            this.OnTemplateAppliedCalled = true;
        }
    }
}
