namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class Decorator : Control
    {
        public static readonly PerspexProperty<Control> ContentProperty =
            PerspexProperty.Register<ContentControl, Control>("Content");

        public Control Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
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
                content.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }

        protected override Size MeasureContent(Size availableSize)
        {
            Control content = this.Content;

            if (content != null)
            {
                content.Measure(availableSize);
                return content.DesiredSize.Value;
            }
            else
            {
                return new Size();
            }
        }
    }
}
