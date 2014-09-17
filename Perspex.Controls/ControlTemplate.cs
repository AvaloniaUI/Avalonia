// -----------------------------------------------------------------------
// <copyright file="ControlTemplate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Styling;

    public class ControlTemplate
    {
        private Func<ITemplatedControl, Control> build;

        public ControlTemplate(Func<ITemplatedControl, Control> build)
        {
            Contract.Requires<NullReferenceException>(build != null);

            this.build = build;
        }

        public static ControlTemplate Create<TControl>(Func<TControl, Control> build)
            where TControl : ITemplatedControl
        {
            Contract.Requires<NullReferenceException>(build != null);

            return new ControlTemplate(c => build((TControl)c));
        }

        public Control Build(ITemplatedControl templatedParent)
        {
            Contract.Requires<NullReferenceException>(templatedParent != null);

            Control root = this.build(templatedParent);
            root.TemplatedParent = templatedParent;
            return root;
        }
    }
}
