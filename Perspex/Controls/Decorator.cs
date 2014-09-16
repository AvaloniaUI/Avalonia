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
            this.GetObservableWithHistory(ContentProperty).Subscribe(x =>
            {
                if (x.Item1 != null)
                {
                    ((IVisual)this).RemoveVisualChild(x.Item1);
                    ((ILogical)this).RemoveLogicalChild(x.Item1);
                }

                if (x.Item2 != null)
                {
                    ((IVisual)this).AddVisualChild(x.Item2);
                    ((ILogical)this).AddLogicalChild(x.Item2);
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

        IEnumerable<IVisual> IVisual.ExistingVisualChildren
        {
            get { return ((IVisual)this).VisualChildren; }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get 
            {
                Control content = this.Content;
                return Enumerable.Repeat(content, content != null ? 1 : 0);
            }
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
