// -----------------------------------------------------------------------
// <copyright file="Decorator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class Decorator : Control
    {
        public static readonly PerspexProperty<Control> ContentProperty =
            PerspexProperty.Register<ContentControl, Control>("Content");

        public static readonly PerspexProperty<Thickness> PaddingProperty =
            PerspexProperty.Register<ContentControl, Thickness>("Padding");

        public Decorator()
        {
            // TODO: Unset old content's visual parent.
            this.GetObservable(ContentProperty).Subscribe(x =>
            {
                if (x != null)
                {
                    x.VisualParent = this;
                }
            });
        }

        public Control Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        public Thickness Padding
        {
            get { return this.GetValue(PaddingProperty); }
            set { this.SetValue(PaddingProperty, value); }
        }

        public override IEnumerable<Visual> VisualChildren
        {
            get 
            {
                Control content = this.Content;
                return Enumerable.Repeat(content, content != null ? 1 : 0);
            }
        }

        protected override Size ArrangeContent(Size finalSize)
        {
            Control content = this.Content;

            if (content != null)
            {
                content.Arrange(new Rect(finalSize).Deflate(this.Padding));
            }

            return finalSize;
        }

        protected override Size MeasureContent(Size availableSize)
        {
            Control content = this.Content;

            if (content != null)
            {
                content.Measure(availableSize);
                return content.DesiredSize.Value.Inflate(this.Padding);
            }
            else
            {
                return new Size();
            }
        }
    }
}
