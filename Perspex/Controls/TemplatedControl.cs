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
    using Splat;

    public class TemplatedControl : Control, IVisual, ITemplatedControl
    {
        public static readonly PerspexProperty<ControlTemplate> TemplateProperty =
            PerspexProperty.Register<TemplatedControl, ControlTemplate>("Template");

        private IVisual visualChild;

        public ControlTemplate Template
        {
            get { return this.GetValue(TemplateProperty); }
            set { this.SetValue(TemplateProperty, value); }
        }

        IEnumerable<IVisual> IVisual.ExistingVisualChildren
        {
            get { return Enumerable.Repeat(this.visualChild, this.visualChild != null ? 1 : 0); }
        }

        IEnumerable<IVisual> ITemplatedControl.VisualChildren
        {
            get 
            {
                var template = this.Template;

                if (this.visualChild == null && template != null)
                {
                    this.Log().Debug(string.Format(
                        "Creating template for {0} (#{1:x8})",
                        this.GetType().Name,
                        this.GetHashCode()));

                    this.visualChild = template.Build(this);
                    this.visualChild.VisualParent = this;
                    this.OnTemplateApplied();
                }

                return Enumerable.Repeat(this.visualChild, this.visualChild != null ? 1 : 0);
            }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get { return ((ITemplatedControl)this).VisualChildren; }
        }

        public sealed override void Render(IDrawingContext context)
        {
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Control child = ((IVisual)this).VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Arrange(new Rect(finalSize));
                return child.ActualSize;
            }
            else
            {
                return new Size();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Control child = ((IVisual)this).VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Measure(availableSize);
                return child.DesiredSize.Value;
            }

            return new Size();
        }

        protected virtual void OnTemplateApplied()
        {
        }
    }
}
