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
    using System.Reactive.Linq;
    using Perspex.Layout;

    public class Decorator : Control, IVisual
    {
        public static readonly PerspexProperty<Control> ContentProperty =
            PerspexProperty.Register<Decorator, Control>("Content");

        public static readonly PerspexProperty<Thickness> PaddingProperty =
            PerspexProperty.Register<Decorator, Thickness>("Padding");

        public Decorator()
        {
            this.GetObservable(ContentProperty).Subscribe(x =>
            {
                this.ClearVisualChildren();

                if (x != null)
                {
                    this.AddVisualChild(x);
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

        protected override Size ArrangeOverride(Size finalSize)
        {
            Control content = this.Content;

            if (content != null)
            {
                content.Arrange(new Rect(finalSize).Deflate(this.Padding));
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureDecorator(this, this.Content, availableSize, this.Padding);
        }
    }
}
