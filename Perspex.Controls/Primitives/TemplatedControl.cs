// -----------------------------------------------------------------------
// <copyright file="TemplatedControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Media;
    using Perspex.Styling;
    using Splat;

    public class TemplatedControl : Control, ITemplatedControl
    {
        public static readonly PerspexProperty<ControlTemplate> TemplateProperty =
            PerspexProperty.Register<TemplatedControl, ControlTemplate>("Template");

        private bool templateApplied;

        public ControlTemplate Template
        {
            get { return this.GetValue(TemplateProperty); }
            set { this.SetValue(TemplateProperty, value); }
        }

        public sealed override void Render(IDrawingContext context)
        {
        }

        protected sealed override void ApplyTemplate()
        {
            if (!this.templateApplied)
            {
                this.ClearVisualChildren();

                if (this.Template != null)
                {
                    this.Log().Debug(
                        "Creating template for {0} (#{1:x8})",
                        this.GetType().Name,
                        this.GetHashCode());

                    var child = this.Template.Build(this);
                    this.AddVisualChild(child);
                    this.OnTemplateApplied();
                }

                this.templateApplied = true;
            }
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

        protected T FindTemplateChild<T>(string id) where T : Control
        {
            return this.GetTemplateControls().OfType<T>().FirstOrDefault(x => x.Id == id);
        }

        protected T GetTemplateChild<T>(string id) where T : Control
        {
            var result = this.FindTemplateChild<T>(id);

            if (result == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Could not find template child '{0}' in template for '{1}'.",
                    id,
                    this.GetType().FullName));
            }

            return result;
        }

        protected virtual void OnTemplateApplied()
        {
        }
    }
}
