// -----------------------------------------------------------------------
// <copyright file="ControlTemplate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Perspex.Controls;

    public class ControlTemplate
    {
        private Func<ITemplatedControl, Control> build;

        public ControlTemplate(Func<ITemplatedControl, Control> build)
        {
            Contract.Requires<NullReferenceException>(build != null);

            this.build = build;
        }

        public Control Build(ITemplatedControl templatedParent)
        {
            Contract.Requires<NullReferenceException>(templatedParent != null);

            Control root = this.build(templatedParent);
            this.SetTemplatedParent(root, templatedParent);
            return root;
        }

        public static ControlTemplate Create<TControl>(Func<TControl, Control> build)
            where TControl : ITemplatedControl
        {
            Contract.Requires<NullReferenceException>(build != null);

            return new ControlTemplate(c => build((TControl)c));
        }

        private void SetTemplatedParent(Control control, ITemplatedControl templatedParent)
        {
            Contract.Requires<NullReferenceException>(control != null);
            Contract.Requires<NullReferenceException>(templatedParent != null);

            control.TemplatedParent = templatedParent;

            foreach (Control child in control.VisualChildren.OfType<Control>())
            {
                this.SetTemplatedParent(child, templatedParent);
            }
        }
    }
}
