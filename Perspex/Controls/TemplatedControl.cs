// -----------------------------------------------------------------------
// <copyright file="TemplatedControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Media;

    public abstract class TemplatedControl : Control
    {
        public static readonly PerspexProperty<Func<TemplatedControl, Visual>> TemplateProperty =
            PerspexProperty.Register<TemplatedControl, Func<TemplatedControl, Visual>>("Template");

        private Visual visualChild;

        public TemplatedControl()
        {
            this.Template = owner => this.DefaultTemplate();
        }

        public Func<TemplatedControl, Visual> Template
        {
            get { return this.GetValue(TemplateProperty); }
            set { this.SetValue(TemplateProperty, value); }
        }

        public override IEnumerable<Visual> VisualChildren
        {
            get 
            {
                var template = this.Template;

                if (this.visualChild == null && template != null)
                {
                    this.visualChild = template(this);
                    this.visualChild.VisualParent = this;
                }

                return Enumerable.Repeat(this.visualChild, this.visualChild != null ? 1 : 0);
            }
        }

        public sealed override void Render(IDrawingContext context)
        {
        }

        protected abstract Visual DefaultTemplate();
    }
}
